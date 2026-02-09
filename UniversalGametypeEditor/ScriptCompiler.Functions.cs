// ScriptCompiler.Functions.cs
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UniversalGametypeEditor
{
    internal partial class ScriptCompiler
    {
        private readonly record struct InlineFuncInfo(
            int ConditionOffset, int ConditionCount,
            int ActionOffset, int ActionCount,
            bool IsResolved
        );

        private readonly Dictionary<string, InlineFuncInfo> _inlineFuncs
            = new Dictionary<string, InlineFuncInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly record struct DeferredFuncCall(int InlineActionIndex, string FuncName);
        private readonly List<DeferredFuncCall> _deferredFuncCalls = new();

        private bool IsInlineFunctionLocalFunction(LocalFunctionStatementSyntax lf)
            => lf.ReturnType != null && lf.ReturnType.ToString() == "void";

        /// <summary>
        /// Call once in Compile(...) AFTER analyzer and BEFORE ProcessMember(...) loop.
        /// This version supports recursion + mutual recursion.
        /// </summary>
        private void Prepass_RegisterInlineFunctions(CompilationUnitSyntax root)
        {
            // Pass 1: predeclare all inline functions so calls inside bodies can resolve by name
            foreach (var member in root.Members)
                Prepass_PredeclareInlineFunctions(member);

            // Pass 2: compile bodies
            foreach (var member in root.Members)
                Prepass_CompileInlineFunctionBodies(member);

            // Patch any calls emitted while functions were unresolved
            PatchDeferredFunctionCalls();
        }

        private void Prepass_PredeclareInlineFunctions(MemberDeclarationSyntax member)
        {
            if (member is ClassDeclarationSyntax c)
            {
                foreach (var m in c.Members) Prepass_PredeclareInlineFunctions(m);
                return;
            }

            if (member is not GlobalStatementSyntax gs) return;
            if (gs.Statement is not LocalFunctionStatementSyntax lf) return;

            if (IsTriggerLocalFunction(lf)) return;
            if (!IsInlineFunctionLocalFunction(lf)) return;

            string name = lf.Identifier.Text;

            if (_inlineFuncs.ContainsKey(name))
                throw new Exception($"Duplicate inline function '{name}'.");

            if (lf.ParameterList?.Parameters.Count > 0)
                throw new Exception($"Inline function '{name}' cannot have parameters yet.");

            if (lf.Body == null)
                throw new Exception($"Inline function '{name}' must have a block body.");

            // Placeholder (unresolved) — offsets/counts filled in during Pass 2
            _inlineFuncs[name] = new InlineFuncInfo(-1, 0, -1, 0, false);
        }

        private void Prepass_CompileInlineFunctionBodies(MemberDeclarationSyntax member)
        {
            if (member is ClassDeclarationSyntax c)
            {
                foreach (var m in c.Members) Prepass_CompileInlineFunctionBodies(m);
                return;
            }

            if (member is not GlobalStatementSyntax gs) return;
            if (gs.Statement is not LocalFunctionStatementSyntax lf) return;

            if (IsTriggerLocalFunction(lf)) return;
            if (!IsInlineFunctionLocalFunction(lf)) return;

            CompileAndResolveInlineFunction(lf);
        }

        private void CompileAndResolveInlineFunction(LocalFunctionStatementSyntax lf)
        {
            string name = lf.Identifier.Text;

            // Already resolved? (Shouldn't happen, but safe)
            if (_inlineFuncs.TryGetValue(name, out var info) && info.IsResolved)
                return;

            int startCond = _conditions.Count;
            int startAct = _actions.Count;

            // allow nested ifs inside the function to create deferred-inlines
            _deferredInlines.Clear();

            _actionBaseStack.Push(startAct);
            _scopeStack.Push(new Dictionary<string, VariableInfo>());

            int condCursor = startCond;
            int actCursor = startAct;
            int condCount = 0;
            int actCount = 0;

            var statements = lf.Body!.Statements;
            for (int i = 0; i < statements.Count; i++)
            {
                bool isLast = (i == statements.Count - 1);
                ProcessStatement(
                    statements[i],
                    ref actCount,
                    ref condCount,
                    ref actCursor,
                    ref condCursor,
                    isTopLevel: true,
                    isLastInBlock: isLast
                );
            }

            EndScope();

            ApplyDeferredInlines_ForCurrentContainer();

            _actionBaseStack.Pop();

            int finalCondCount = _conditions.Count - startCond;
            int finalActCount = _actions.Count - startAct;

            _inlineFuncs[name] = new InlineFuncInfo(
                startCond, finalCondCount,
                startAct, finalActCount,
                true
            );
        }

        /// <summary>
        /// Call at the top of ProcessInvocation(...). Returns true if handled as an inline function call.
        /// Supports recursion/mutual recursion by emitting a placeholder inline and patching later.
        /// </summary>
        private bool TryEmitInlineFunctionCall(InvocationExpressionSyntax invocation, ref int actionOffset)
        {
            string? name = GetInvokedName(invocation.Expression);
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (!_inlineFuncs.TryGetValue(name, out var fn))
                return false;

            if (invocation.ArgumentList?.Arguments.Count > 0)
                throw new Exception($"Inline function '{name}' cannot take arguments yet.");

            int insertIndex = actionOffset;

            // If already resolved, emit correct inline immediately
            if (fn.IsResolved)
            {
                string bits = BuildInlineBinary(fn.ConditionOffset, fn.ConditionCount, fn.ActionOffset, fn.ActionCount);
                _actions.Insert(insertIndex, new ActionObject("Inline", new List<string> { bits }));
            }
            else
            {
                // Placeholder inline — patch later
                string bits = BuildInlineBinary(0, 0, 0, 0);
                _actions.Insert(insertIndex, new ActionObject("Inline", new List<string> { bits }));
                _deferredFuncCalls.Add(new DeferredFuncCall(insertIndex, name));
            }

            actionOffset++;
            return true;
        }

        private void PatchDeferredFunctionCalls()
        {
            for (int i = 0; i < _deferredFuncCalls.Count; i++)
            {
                var call = _deferredFuncCalls[i];

                if (!_inlineFuncs.TryGetValue(call.FuncName, out var fn) || !fn.IsResolved)
                    throw new Exception($"Inline function '{call.FuncName}' was called but never resolved.");

                string bits = BuildInlineBinary(fn.ConditionOffset, fn.ConditionCount, fn.ActionOffset, fn.ActionCount);
                _actions[call.InlineActionIndex].Parameters[0] = bits;
            }

            _deferredFuncCalls.Clear();
        }

        /// <summary>
        /// Same inline-patching logic you already do for if-block extraction.
        /// Factor this out from trigger compilation so functions can use it too.
        /// </summary>
        private void ApplyDeferredInlines_ForCurrentContainer()
        {
            foreach (var patch in _deferredInlines)
            {
                int condOffset = _conditions.Count;
                int condCount = patch.Conditions.Count;
                _conditions.AddRange(patch.Conditions);

                int actOffset = _actions.Count;
                int actCount = patch.Actions.Count;
                _actions.AddRange(patch.Actions);

                string fixedInline = BuildInlineBinary(condOffset, condCount, actOffset, actCount);
                _actions[patch.InlineActionIndex].Parameters[0] = fixedInline;
            }

            _deferredInlines.Clear();
        }
    }
}

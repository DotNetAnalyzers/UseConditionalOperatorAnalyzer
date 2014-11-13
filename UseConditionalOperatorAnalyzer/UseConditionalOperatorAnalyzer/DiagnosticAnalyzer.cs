using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UseConditionalOperatorAnalyzer
{
    using Analyzer = Predicate<IfStatementSyntax>;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseConditionalOperatorAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableArray<Analyzer> Tests =
            ImmutableArray.Create<Analyzer>(CanSimplifyAssignment, CanSimplifyReturn);

        public const string DiagnosticId = "UseConditionalOperator";
        internal const string Title = "Replace with conditional operator";
        internal const string MessageFormat = "If statement can be replaced with a conditional operator.";
        internal const string Category = "Syntax";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticId,
                Title,
                MessageFormat,
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IfStatement);
        }

        private static bool CanSimplifyAssignment(IfStatementSyntax ifStatement)
        {
            var truePartStmt = ifStatement.Statement as ExpressionStatementSyntax;
            if (truePartStmt == null) return false;

            var truePartAssignment = (truePartStmt.Expression as AssignmentExpressionSyntax)?.Left;
            if (truePartAssignment == null) return false;

            var falsePartStmt = ifStatement?.Else?.Statement as ExpressionStatementSyntax;
            if (falsePartStmt == null) return false;

            var falsePartAssignment = (falsePartStmt.Expression as AssignmentExpressionSyntax)?.Left;
            if (falsePartAssignment == null) return false;

            if (!truePartAssignment.IsEquivalentTo(falsePartAssignment)) return false;

            return true;
        }

        private static bool CanSimplifyReturn(IfStatementSyntax ifStatement)
        {
            if ((SyntaxKind)ifStatement.Statement.RawKind != SyntaxKind.ReturnStatement) return false;
            if ((SyntaxKind)ifStatement?.Else?.Statement.RawKind != SyntaxKind.ReturnStatement) return false;

            return true;
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            if (ifStatement.Else == null || !Tests.Any(p => p(ifStatement))) return;

            var diag = Diagnostic.Create(Rule, ifStatement.GetLocation());
            context.ReportDiagnostic(diag);
        }
    }
}

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace UseConditionalOperatorAnalyzer
{
    [ExportCodeFixProvider("UseConditionalOperatorAnalyzerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UseConditionalOperatorAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(UseConditionalOperatorAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();

            context.RegisterFix(
                CodeAction.Create("Replace with conditional operator", c => MakeConditionalAsync(context.Document, declaration, c)),
                diagnostic);
        }

        private static TNode ApplyFormatting<TNode>(TNode node)
            where TNode : SyntaxNode
        {
            return node.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static TNode CreateNodeWithSourceFormatting<TNode>(SyntaxNode sourceNode, Func<TNode> factory)
            where TNode : SyntaxNode
        {
            return
                factory()
                    .WithLeadingTrivia(sourceNode.GetLeadingTrivia())
                    .WithTrailingTrivia(sourceNode.GetTrailingTrivia());
        }

        private static ExpressionSyntax ExtractSimpleAssignmentRight(AssignmentExpressionSyntax assignment)
        {
            switch (assignment.CSharpKind())
            {
                case SyntaxKind.AddAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, assignment.Left, assignment.Right);
                case SyntaxKind.SubtractAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, assignment.Left, assignment.Right);
                case SyntaxKind.MultiplyAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.MultiplyExpression, assignment.Left, assignment.Right);
                case SyntaxKind.DivideAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression, assignment.Left, assignment.Right);
                case SyntaxKind.ModuloAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.ModuloExpression, assignment.Left, assignment.Right);
                case SyntaxKind.AndAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.AndAssignmentExpression, assignment.Left, assignment.Right);
                case SyntaxKind.OrAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.OrAssignmentExpression, assignment.Left, assignment.Right);
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.ExclusiveOrExpression, assignment.Left, assignment.Right);
                case SyntaxKind.LeftShiftAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.LeftShiftExpression, assignment.Left, assignment.Right);
                case SyntaxKind.RightShiftAssignmentExpression:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.RightShiftExpression, assignment.Left, assignment.Right);
                default:
                    return assignment.Right;
            }
        }

        private static LocalDeclarationStatementSyntax CreateVarDeclaration(string symbolName, ExpressionSyntax expression)
        {
            return
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var").WithTrailingTrivia(SyntaxFactory.Space),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory
                                .VariableDeclarator(symbolName)
                                .WithTrailingTrivia(SyntaxFactory.Space)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(expression)))));
        }

        private async static Task<SyntaxNode> ApplyAssignmentCodeFix(Document sourceDocument, IfStatementSyntax ifStatement)
        {
            var truePartStmt = ifStatement.Statement as ExpressionStatementSyntax;
            var truePartExpr = truePartStmt.Expression as AssignmentExpressionSyntax;
            var falsePartStmt = ifStatement.Else.Statement as ExpressionStatementSyntax;
            var falsePartExpr = falsePartStmt.Expression as AssignmentExpressionSyntax;

            var conditionalExpr =
                SyntaxFactory
                    .ConditionalExpression(
                        ApplyFormatting(SyntaxFactory.ParenthesizedExpression(ifStatement.Condition)),
                        ApplyFormatting(ExtractSimpleAssignmentRight(truePartExpr)),
                        ApplyFormatting(ExtractSimpleAssignmentRight(falsePartExpr)));

            var assignmentExpr =
                SyntaxFactory
                    .AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, truePartExpr.Left, conditionalExpr);

            var docRoot = await sourceDocument.GetSyntaxRootAsync();

            var semanticModel = await sourceDocument.GetSemanticModelAsync();
            var targetSymbol = semanticModel.GetSymbolInfo(truePartExpr.Left).Symbol;

            if (targetSymbol.Kind == SymbolKind.Local)
            {
                var declarationSyntax = targetSymbol.DeclaringSyntaxReferences[0].GetSyntax().Parent.Parent;
                var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(declarationSyntax, ifStatement);
                var variableIsRead = dataFlowAnalysis.ReadInside.Any(v => v.Name.Equals(targetSymbol.Name));

                if (!variableIsRead)
                {
                    var newRoot = docRoot.TrackNodes(declarationSyntax, ifStatement);

                    var newDeclarationSyntax = newRoot.GetCurrentNode(declarationSyntax);
                    newRoot = newRoot.RemoveNode(newDeclarationSyntax, SyntaxRemoveOptions.KeepNoTrivia);

                    var declarationStmt =
                        CreateNodeWithSourceFormatting(
                            ifStatement,
                            () => CreateVarDeclaration(targetSymbol.Name, conditionalExpr));

                    var newIfStatement = newRoot.GetCurrentNode(ifStatement);
                    return newRoot.ReplaceNode(newIfStatement as CSharpSyntaxNode, declarationStmt);
                }
            }

            var assignmentStmt =
                CreateNodeWithSourceFormatting(
                    ifStatement,
                    () => SyntaxFactory.ExpressionStatement(assignmentExpr));

            return docRoot.ReplaceNode(ifStatement as CSharpSyntaxNode, assignmentStmt);
        }

        private async static Task<SyntaxNode> ApplyReturnCodeFix(Document sourceDocument, IfStatementSyntax ifStatement)
        {
            var truePartStmt = ifStatement.Statement as ReturnStatementSyntax;
            var falsePartStmt = ifStatement.Else.Statement as ReturnStatementSyntax;

            var conditionalExpr =
                SyntaxFactory.ConditionalExpression(
                    ApplyFormatting(SyntaxFactory.ParenthesizedExpression(ifStatement.Condition)),
                    ApplyFormatting(truePartStmt.Expression),
                    ApplyFormatting(falsePartStmt.Expression));

            var returnStmt =
                CreateNodeWithSourceFormatting(
                    ifStatement,
                    () => SyntaxFactory.ReturnStatement(conditionalExpr));

            var docRoot = await sourceDocument.GetSyntaxRootAsync();
            return docRoot.ReplaceNode(ifStatement as CSharpSyntaxNode, returnStmt);
        }

        private async Task<Document> MakeConditionalAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            SyntaxNode newRoot = null;

            switch ((SyntaxKind)ifStatement.Statement.RawKind)
            {
                case SyntaxKind.ExpressionStatement:
                    newRoot = await ApplyAssignmentCodeFix(document, ifStatement);
                    break;

                case SyntaxKind.ReturnStatement:
                    newRoot = await ApplyReturnCodeFix(document, ifStatement);
                    break;
            }

            return newRoot == null ? document : document.WithSyntaxRoot(newRoot);
        }
    }
}
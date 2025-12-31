using VectraCompiler.AST.Models.Declarations;
using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.AST.Models.Statements;

namespace VectraCompiler.AST.Models;

public interface IAstVisitor<out T>
{
    #region DeclarationVisitors

    T VisitClassDeclaration(ClassDeclarationNode node);
    T VisitConstructorDeclaration(ConstructorDeclarationNode node);
    T VisitFieldDeclaration(FieldDeclarationNode node);
    T VisitMethodDeclaration(MethodDeclarationNode node);
    T VisitPropertyDeclaration(PropertyDeclarationNode node);

    #endregion

    #region ExpressionVisitors

    T VisitBinaryExpression(BinaryExpressionNode node);
    T VisitCallExpression(CallExpressionNode node);
    T VisitIdentifierExpression(IdentifierExpressionNode node);
    T VisitLiteralExpression(LiteralExpressionNode node);
    T VisitNewExpression(NewExpressionNode node);
    
    #endregion

    #region StatementVisitors

    T VisitExpressionStatement(ExpressionStatementNode node);
    T VisitReturnStatement(ReturnStatementNode node);
    T VisitVariableDeclarationStatement(VariableDeclarationStatementNode node);

    #endregion
}
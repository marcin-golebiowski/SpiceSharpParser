﻿using System;
using System.Collections.Generic;
using SpiceSharpParser.Lexers.Netlist.Spice;

namespace SpiceSharpParser.Parsers.Netlist.Spice
{
    /// <summary>
    /// A parser tree generator for Spice netlist based on grammar from SpiceGrammar.txt.
    /// It's a manualy written LL(*) parser.
    /// </summary>
    public class ParseTreeGenerator
    {
        private Dictionary<string, Action<Stack<ParseTreeNode>, ParseTreeNonTerminalNode, SpiceToken[], int>> parsers = new Dictionary<string, Action<Stack<ParseTreeNode>, ParseTreeNonTerminalNode, SpiceToken[], int>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseTreeGenerator"/> class.
        /// </summary>
        /// <param name="isNewLineRequiredAtTheEnd">Is NEWLINE required at the end?</param>
        public ParseTreeGenerator(bool isNewLineRequiredAtTheEnd = false)
        {
            IsNewLineRequiredAtTheEnd = isNewLineRequiredAtTheEnd;

            parsers.Add(Symbols.Netlist, ReadNetlist);
            parsers.Add(Symbols.NetlistWithoutTitle, ReadNetlistWithoutTitle);
            parsers.Add(Symbols.NetlistEnding, ReadNetlistEnding);
            parsers.Add(Symbols.Statements, ReadStatements);
            parsers.Add(Symbols.Statement, ReadStatement);
            parsers.Add(Symbols.CommentLine, ReadCommentLine);
            parsers.Add(Symbols.Subckt, ReadSubckt);
            parsers.Add(Symbols.SubcktEnding, ReadSubcktEnding);
            parsers.Add(Symbols.Component, ReadComponent);
            parsers.Add(Symbols.Control, ReadControl);
            parsers.Add(Symbols.Model, ReadModel);
            parsers.Add(Symbols.Parameters, ReadParameters);
            parsers.Add(Symbols.Parameter, ReadParameter);
            parsers.Add(Symbols.ParameterSingle, ReadParameterSingle);
            parsers.Add(Symbols.ParameterBracket, ReadParameterBracket);
            parsers.Add(Symbols.ParameterBracketContent, ReadParameterBracketContent);
            parsers.Add(Symbols.ParameterEqual, ReadParameterEqual);
            parsers.Add(Symbols.ParameterEqualSingle, ReadParameterEqualSingle);
            parsers.Add(Symbols.Vector, ReadVector);
            parsers.Add(Symbols.VectorContinue, ReadVectorContinue);
            parsers.Add(Symbols.NewLine, ReadNewLine);
        }

        protected bool IsNewLineRequiredAtTheEnd { get; }

        /// <summary>
        /// Generates a parse tree for SPICE grammar.
        /// </summary>
        /// <param name="tokens">An array of tokens.</param>
        /// <param name="rootSymbol">A root symbol of parse tree.</param>
        /// <returns>
        /// A parse tree.
        /// </returns>
        public ParseTreeNonTerminalNode GetParseTree(SpiceToken[] tokens, string rootSymbol = Symbols.Netlist)
        {
            if (tokens == null)
            {
                throw new System.ArgumentNullException(nameof(tokens));
            }

            var stack = new Stack<ParseTreeNode>();

            var root = CreateNonTerminalNode(rootSymbol, null);
            stack.Push(root);

            int currentTokenIndex = 0;

            while (stack.Count > 0)
            {
                var currentNode = stack.Pop();
                if (currentNode is ParseTreeNonTerminalNode ntn)
                {
                    if (parsers.ContainsKey(ntn.Name))
                    {
                        parsers[ntn.Name](stack, ntn, tokens, currentTokenIndex);
                    }
                    else
                    {
                        throw new ParseException("Unknown non-terminal found while parsing." + ntn.Name, tokens[currentTokenIndex].LineNumber);
                    }
                }

                if (currentNode is ParseTreeTerminalNode tn)
                {
                    if (currentTokenIndex >= tokens.Length)
                    {
                        throw new ParseException("End of tokens. Expected token type: " + tn.Token.SpiceTokenType + " line=" + tokens[tokens.Length - 1].LineNumber, tokens[tokens.Length - 1].LineNumber);
                    }

                    if (tn.Token.SpiceTokenType == tokens[currentTokenIndex].SpiceTokenType
                        && (tn.Token.Lexem == null || tn.Token.Lexem == tokens[currentTokenIndex].Lexem))
                    {
                        tn.Token.UpdateLexem(tokens[currentTokenIndex].Lexem);
                        tn.Token.UpdateLineNumber(tokens[currentTokenIndex].LineNumber);
                        currentTokenIndex++;
                    }
                    else
                    {
                        throw new ParseException(string.Format("Unexpected token: '{0}' of type: {1}. Expected token type: {2} line={3}", tokens[currentTokenIndex].Lexem, tokens[currentTokenIndex].SpiceTokenType, tn.Token.SpiceTokenType, tokens[currentTokenIndex].LineNumber), tokens[currentTokenIndex].LineNumber);
                    }
                }
            }

            if (currentTokenIndex != tokens.Length)
            {
                throw new ParseException("There are pending tokens to process", tokens[currentTokenIndex].LineNumber);
            }

            return root;
        }

        /// <summary>
        /// Reads <see cref="Symbols.SubcktEnding"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadSubcktEnding(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];
            var nextToken = tokens[currentTokenIndex + 1];

            if (currentToken.Is(SpiceTokenType.ENDS))
            {
                if (nextToken.Is(SpiceTokenType.WORD))
                {
                    PushProductionExpression(
                        stack,
                        CreateTerminalNode(SpiceTokenType.ENDS, currentNode),
                        CreateTerminalNode(SpiceTokenType.WORD, currentNode));
                }
                else
                {
                    PushProductionExpression(
                         stack,
                         CreateTerminalNode(SpiceTokenType.ENDS, currentNode));
                }
            }
            else
            {
                throw new ParseException("Error during parsing subcircuit. Expected .ENDS. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.NewLine"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadNewLine(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            if (currentTokenIndex > tokens.Length - 1)
            {
                throw new ParseException("End of tokens. New line not found", tokens[tokens.Length - 1].LineNumber);
            }

            var currentToken = tokens[currentTokenIndex];
            if (currentToken.Is(SpiceTokenType.NEWLINE))
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(SpiceTokenType.NEWLINE, currentNode));
            }
            else
            {
                throw new ParseException("Newline was expected. Other token was found.", currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.Netlist"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadNetlist(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];

            if (currentToken.Is(SpiceTokenType.EOF))
            {
                PushProductionExpression(
                    stack,
                    CreateNonTerminalNode(Symbols.NetlistEnding, currentNode));
                return;
            }

            if (currentToken.Is(SpiceTokenType.NEWLINE))
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(SpiceTokenType.NEWLINE, currentNode),
                    CreateNonTerminalNode(Symbols.Statements, currentNode),
                    CreateNonTerminalNode(Symbols.NetlistEnding, currentNode));
            }
            else
            {
                if (tokens[currentTokenIndex + 1].Is(SpiceTokenType.EOF) && !IsNewLineRequiredAtTheEnd)
                {
                    PushProductionExpression(
                     stack,
                     CreateTerminalNode(SpiceTokenType.TITLE, currentNode),
                     CreateNonTerminalNode(Symbols.NetlistEnding, currentNode));
                }
                else
                {
                    PushProductionExpression(
                        stack,
                        CreateTerminalNode(SpiceTokenType.TITLE, currentNode),
                        CreateTerminalNode(SpiceTokenType.NEWLINE, currentNode),
                        CreateNonTerminalNode(Symbols.Statements, currentNode),
                        CreateNonTerminalNode(Symbols.NetlistEnding, currentNode));
                }
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.NetlistWithoutTitle"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadNetlistWithoutTitle(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            PushProductionExpression(
                stack,
                CreateNonTerminalNode(Symbols.Statements, currentNode),
                CreateNonTerminalNode(Symbols.NetlistEnding, currentNode));
        }

        /// <summary>
        /// Reads <see cref="Symbols.NetlistEnding"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadNetlistEnding(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];

            if (currentToken.Is(SpiceTokenType.END))
            {
                if (IsNewLineRequiredAtTheEnd)
                {
                    PushProductionExpression(
                                stack,
                                CreateTerminalNode(SpiceTokenType.END, current),
                                CreateTerminalNode(SpiceTokenType.NEWLINE, current),
                                CreateTerminalNode(SpiceTokenType.EOF, current));
                }
                else
                {
                    if (currentTokenIndex + 1 < tokens.Length)
                    {
                        if (tokens[currentTokenIndex + 1].Is(SpiceTokenType.NEWLINE))
                        {
                            PushProductionExpression(
                                stack,
                                CreateTerminalNode(SpiceTokenType.END, current),
                                CreateTerminalNode(SpiceTokenType.NEWLINE, current),
                                CreateTerminalNode(SpiceTokenType.EOF, current));
                        }
                        else
                        {
                            if (tokens[currentTokenIndex + 1].Is(SpiceTokenType.EOF))
                            {
                                PushProductionExpression(
                                    stack,
                                    CreateTerminalNode(SpiceTokenType.END, current),
                                    CreateTerminalNode(SpiceTokenType.EOF, current));
                            }
                            else
                            {
                                throw new ParseException("Netlist ending - wrong ending", currentToken.LineNumber);
                            }
                        }
                    }
                    else
                    {
                        PushProductionExpression(
                           stack,
                           CreateTerminalNode(SpiceTokenType.END, current));
                    }
                }
            }
            else
            {
                if (currentToken.Is(SpiceTokenType.EOF))
                {
                    PushProductionExpression(
                            stack,
                            CreateTerminalNode(SpiceTokenType.EOF, current));
                }
                else
                {
                    throw new ParseException("Netlist ending - wrong ending", currentToken.LineNumber);
                }
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.Statements"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadStatements(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];

            if (currentToken.Is(SpiceTokenType.DOT)
                || currentToken.Is(SpiceTokenType.WORD)
                || currentToken.Is(SpiceTokenType.COMMENT)
                || currentToken.Is(SpiceTokenType.ENDL)
                || currentToken.Is(SpiceTokenType.IF)
                || currentToken.Is(SpiceTokenType.ELSE)
                || currentToken.Is(SpiceTokenType.ELSE_IF)
                || currentToken.Is(SpiceTokenType.ENDIF))
            {
                PushProductionExpression(
                            stack,
                            CreateNonTerminalNode(Symbols.Statement, current),
                            CreateNonTerminalNode(Symbols.Statements, current));
            }
            else if (currentToken.Is(SpiceTokenType.NEWLINE))
            {
                PushProductionExpression(
                         stack,
                         CreateTerminalNode(SpiceTokenType.NEWLINE, current),
                         CreateNonTerminalNode(Symbols.Statements, current));
            }
            else if (currentToken.Is(SpiceTokenType.END))
            {
                // follow - do nothing
            }
            else if (currentToken.Is(SpiceTokenType.ENDS))
            {
                // follow - do nothing
            }
            else if (currentToken.Is(SpiceTokenType.EOF))
            {
                // follow - do nothing
            }
            else
            {
                throw new ParseException("Error during parsing statements. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.Statement"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadStatement(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];
            var nextToken = tokens[currentTokenIndex + 1];

            if (currentToken.Is(SpiceTokenType.WORD))
            {
                PushProductionExpression(
                    stack,
                    CreateNonTerminalNode(Symbols.Component, current),
                    CreateTerminalNode(SpiceTokenType.NEWLINE, current));
            }
            else if (currentToken.Is(SpiceTokenType.DOT))
            {
                if (nextToken.Is(SpiceTokenType.WORD))
                {
                    if (nextToken.Equal("subckt", true))
                    {
                        PushProductionExpression(
                            stack,
                            CreateNonTerminalNode(Symbols.Subckt, current),
                            CreateTerminalNode(SpiceTokenType.NEWLINE, current));
                    }
                    else if (nextToken.Equal("model", true))
                    {
                        PushProductionExpression(
                            stack,
                            CreateNonTerminalNode(Symbols.Model, current),
                            CreateTerminalNode(SpiceTokenType.NEWLINE, current));
                    }
                    else
                    {
                        PushProductionExpression(
                            stack,
                            CreateNonTerminalNode(Symbols.Control, current),
                            CreateTerminalNode(SpiceTokenType.NEWLINE, current));
                    }
                }
                else
                {
                    throw new ParseException("Error during parsing a statement. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
                }
            }
            else if (currentToken.Is(SpiceTokenType.COMMENT))
            {
                PushProductionExpression(
                    stack,
                    CreateNonTerminalNode(Symbols.CommentLine, current),
                    CreateTerminalNode(SpiceTokenType.NEWLINE, current));
            }
            else
            {
                if (currentToken.Is(SpiceTokenType.ENDL)
                    || currentToken.Is(SpiceTokenType.IF)
                    || currentToken.Is(SpiceTokenType.ELSE_IF)
                    || currentToken.Is(SpiceTokenType.ELSE)
                    || currentToken.Is(SpiceTokenType.ENDIF))
                {
                    PushProductionExpression(
                           stack,
                           CreateNonTerminalNode(Symbols.Control, current),
                           CreateTerminalNode(SpiceTokenType.NEWLINE, current));
                }
                else
                {
                    throw new ParseException(string.Format("Error during parsing a statement. Unexpected token: '{0}' of type:{1} line={2}", currentToken.Lexem, currentToken.SpiceTokenType, currentToken.LineNumber), currentToken.LineNumber);
                }
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.Vector"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadVector(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            PushProductionExpression(
                stack,
                CreateNonTerminalNode(Symbols.ParameterSingle, current),
                CreateTerminalNode(SpiceTokenType.COMMA, current, ","),
                CreateNonTerminalNode(Symbols.ParameterSingle, current),
                CreateNonTerminalNode(Symbols.VectorContinue, current));
        }

        /// <summary>
        /// Reads <see cref="Symbols.VectorContinue"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadVectorContinue(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            if (currentTokenIndex > tokens.Length - 1)
            {
                return; // empty
            }

            var currentToken = tokens[currentTokenIndex];

            if (currentToken.Is(SpiceTokenType.DELIMITER) && currentToken.Lexem == ")")
            {
                // follow
            }
            else
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(SpiceTokenType.COMMA, current, ","),
                    CreateNonTerminalNode(Symbols.ParameterSingle, current),
                    CreateNonTerminalNode(Symbols.VectorContinue, current));
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.CommentLine"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadCommentLine(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            PushProductionExpression(
                stack,
                CreateTerminalNode(SpiceTokenType.COMMENT, current));
        }

        /// <summary>
        /// Reads <see cref="Symbols.Subckt"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadSubckt(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];
            var nextToken = tokens[currentTokenIndex + 1];

            if (currentToken.Is(SpiceTokenType.DOT)
                && nextToken.Is(SpiceTokenType.WORD)
                && nextToken.Equal("subckt", true))
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(currentToken.SpiceTokenType, current, currentToken.Lexem),
                    CreateTerminalNode(nextToken.SpiceTokenType, current, nextToken.Lexem),
                    CreateTerminalNode(SpiceTokenType.WORD, current),
                    CreateNonTerminalNode(Symbols.Parameters, current),
                    CreateTerminalNode(SpiceTokenType.NEWLINE, current),
                    CreateNonTerminalNode(Symbols.Statements, current),
                    CreateNonTerminalNode(Symbols.SubcktEnding, current));
            }
            else
            {
                throw new ParseException("Error during parsing a subcircuit. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.Parameters"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadParameters(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            if (currentTokenIndex > tokens.Length - 1)
            {
                // empty
                return;
            }

            var currentToken = tokens[currentTokenIndex];

            if (currentToken.Is(SpiceTokenType.WORD)
                || currentToken.Is(SpiceTokenType.VALUE)
                || currentToken.Is(SpiceTokenType.DOUBLE_QUOTED_STRING)
                || currentToken.Is(SpiceTokenType.SINGLE_QUOTED_STRING)
                || currentToken.Is(SpiceTokenType.IDENTIFIER)
                || currentToken.Is(SpiceTokenType.REFERENCE)
                || currentToken.Is(SpiceTokenType.EXPRESSION_BRACKET)
                || currentToken.Is(SpiceTokenType.EXPRESSION_SINGLE_QUOTES)
                || currentToken.Is(SpiceTokenType.PERCENT))
            {
                PushProductionExpression(
                    stack,
                    CreateNonTerminalNode(Symbols.Parameter, current),
                    CreateNonTerminalNode(Symbols.Parameters, current));
            }
            else if (currentToken.Is(SpiceTokenType.EOF))
            {
                // follow - do nothing
            }
            else if (currentToken.Is(SpiceTokenType.NEWLINE))
            {
                // follow - do nothing
            }
            else if (currentToken.Is(SpiceTokenType.DELIMITER) && currentToken.Lexem == ")")
            {
                // follow - do nothing
            }
            else if (currentToken.Is(SpiceTokenType.COMMENT_HSPICE) || currentToken.Is(SpiceTokenType.COMMENT_PSPICE))
            {
                // follow - do nothing
            }
            else
            {
                throw new ParseException(
                    string.Format("Error during parsing parameters. Unexpected token: '{0}' of type {1} line={2}", currentToken.Lexem,  currentToken.SpiceTokenType, currentToken.LineNumber), currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.ParameterEqual"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadParameterEqual(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];
            var nextToken = tokens[currentTokenIndex + 1];

            if (currentToken.Is(SpiceTokenType.WORD)
                || currentToken.Is(SpiceTokenType.IDENTIFIER))
            {
                if (nextToken.Is(SpiceTokenType.EQUAL))
                {
                    stack.Push(CreateNonTerminalNode(Symbols.ParameterEqualSingle, currentNode));
                }
                else if (nextToken.Is(SpiceTokenType.DELIMITER) && nextToken.Equal("(", true))
                {
                    if ((tokens.Length > currentTokenIndex + 3) && tokens[currentTokenIndex + 2].Lexem == ")" && tokens[currentTokenIndex + 3].Lexem == "=")
                    {
                        PushProductionExpression(
                            stack,
                            CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                            CreateTerminalNode(SpiceTokenType.DELIMITER, currentNode, "("),
                            CreateTerminalNode(SpiceTokenType.DELIMITER, currentNode, ")"),
                            CreateTerminalNode(SpiceTokenType.EQUAL, currentNode),
                            CreateNonTerminalNode(Symbols.ParameterSingle, currentNode));
                    }
                    else if ((tokens.Length > currentTokenIndex + 4) && tokens[currentTokenIndex + 3].Lexem == ")" && tokens[currentTokenIndex + 4].Lexem == "=")
                    {
                        PushProductionExpression(
                            stack,
                            CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                            CreateTerminalNode(SpiceTokenType.DELIMITER, currentNode, "("),
                            CreateNonTerminalNode(Symbols.ParameterSingle, currentNode),
                            CreateTerminalNode(SpiceTokenType.DELIMITER, currentNode, ")"),
                            CreateTerminalNode(SpiceTokenType.EQUAL, currentNode),
                            CreateNonTerminalNode(Symbols.ParameterSingle, currentNode));
                    }
                    else
                    {
                        PushProductionExpression(
                            stack,
                            CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                            CreateTerminalNode(SpiceTokenType.DELIMITER, currentNode, "("),
                            CreateNonTerminalNode(Symbols.Vector, currentNode),
                            CreateTerminalNode(SpiceTokenType.DELIMITER, currentNode, ")"),
                            CreateTerminalNode(SpiceTokenType.EQUAL, currentNode),
                            CreateNonTerminalNode(Symbols.ParameterSingle, currentNode));
                    }
                }
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.ParameterEqualSingle"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadParameterEqualSingle(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            PushProductionExpression(
                stack,
                CreateTerminalNode(SpiceTokenType.WORD, current),
                CreateTerminalNode(SpiceTokenType.EQUAL, current),
                CreateNonTerminalNode(Symbols.ParameterSingle, current));
        }

        /// <summary>
        /// Reads <see cref="Symbols.ParameterBracketContent"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadParameterBracketContent(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            PushProductionExpression(
                stack,
                CreateNonTerminalNode(Symbols.Parameters, currentNode));
        }

        /// <summary>
        /// Reads <see cref="Symbols.Parameter"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadParameter(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];
            if (currentTokenIndex == tokens.Length - 1)
            {
                if (currentToken.Is(SpiceTokenType.VALUE)
                        || currentToken.Is(SpiceTokenType.SINGLE_QUOTED_STRING)
                        || currentToken.Is(SpiceTokenType.DOUBLE_QUOTED_STRING)
                        || currentToken.Is(SpiceTokenType.IDENTIFIER)
                        || currentToken.Is(SpiceTokenType.REFERENCE)
                        || currentToken.Is(SpiceTokenType.EXPRESSION_BRACKET)
                        || currentToken.Is(SpiceTokenType.EXPRESSION_SINGLE_QUOTES)
                        || currentToken.Is(SpiceTokenType.PERCENT))
                {
                    PushProductionExpression(
                        stack,
                        CreateNonTerminalNode(Symbols.ParameterSingle, currentNode));
                    return;
                }
                else
                {
                    throw new ParseException("Error during parsing a paremeter. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
                }
            }

            var nextToken = tokens[currentTokenIndex + 1];

            if (nextToken.Is(SpiceTokenType.COMMA))
            {
                PushProductionExpression(
                    stack,
                    CreateNonTerminalNode(Symbols.Vector, currentNode));
            }
            else
            {
                if (currentToken.Is(SpiceTokenType.WORD) || currentToken.Is(SpiceTokenType.IDENTIFIER))
                {
                    if (nextToken.Is(SpiceTokenType.EQUAL))
                    {
                        stack.Push(CreateNonTerminalNode(Symbols.ParameterEqual, currentNode));
                    }
                    else if (nextToken.Is(SpiceTokenType.DELIMITER) && nextToken.Equal("(", true))
                    {
                        if (IsEqualTokens(tokens, currentTokenIndex))
                        {
                            PushProductionExpression(
                                stack,
                                CreateNonTerminalNode(Symbols.ParameterEqual, currentNode));
                        }
                        else
                        {
                            PushProductionExpression(
                                stack,
                                CreateNonTerminalNode(Symbols.ParameterBracket, currentNode));
                        }
                    }
                    else
                    {
                        PushProductionExpression(
                            stack,
                            CreateNonTerminalNode(Symbols.ParameterSingle, currentNode));
                    }
                }
                else
                {
                    if (currentToken.Is(SpiceTokenType.VALUE)
                        || currentToken.Is(SpiceTokenType.SINGLE_QUOTED_STRING)
                        || currentToken.Is(SpiceTokenType.DOUBLE_QUOTED_STRING)
                        || currentToken.Is(SpiceTokenType.IDENTIFIER)
                        || currentToken.Is(SpiceTokenType.REFERENCE)
                        || currentToken.Is(SpiceTokenType.EXPRESSION_BRACKET)
                        || currentToken.Is(SpiceTokenType.EXPRESSION_SINGLE_QUOTES)
                        || currentToken.Is(SpiceTokenType.PERCENT))
                    {
                        PushProductionExpression(
                            stack,
                            CreateNonTerminalNode(Symbols.ParameterSingle, currentNode));
                    }
                    else
                    {
                        throw new ParseException("Error during parsing a paremeter. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
                    }
                }
            }
        }

        private static bool IsEqualTokens(SpiceToken[] tokens, int currentTokenIndex)
        {
            while (tokens.Length > currentTokenIndex && tokens[currentTokenIndex].Lexem != ")")
            {
                currentTokenIndex += 1;
            }

            if (currentTokenIndex + 1 >= tokens.Length - 1)
            {
                return false;
            }

            return tokens[currentTokenIndex + 1].Lexem == "=";
        }

        /// <summary>
        /// Reads <see cref="Symbols.ParameterBracket"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadParameterBracket(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];

            if (currentToken.Is(SpiceTokenType.WORD) || currentToken.Is(SpiceTokenType.IDENTIFIER))
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                    CreateTerminalNode(SpiceTokenType.DELIMITER, currentNode, "("),
                    CreateNonTerminalNode(Symbols.ParameterBracketContent, currentNode),
                    CreateTerminalNode(SpiceTokenType.DELIMITER, currentNode, ")"));
            }
            else
            {
                throw new ParseException("Error during parsing a bracket parameter. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.ParameterSingle"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="current">A reference to the non-terminal node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadParameterSingle(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode current, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];

            if (currentToken.Is(SpiceTokenType.WORD)
                || currentToken.Is(SpiceTokenType.VALUE)
                || currentToken.Is(SpiceTokenType.SINGLE_QUOTED_STRING)
                || currentToken.Is(SpiceTokenType.DOUBLE_QUOTED_STRING)
                || currentToken.Is(SpiceTokenType.IDENTIFIER)
                || currentToken.Is(SpiceTokenType.REFERENCE)
                || currentToken.Is(SpiceTokenType.EXPRESSION_BRACKET)
                || currentToken.Is(SpiceTokenType.EXPRESSION_SINGLE_QUOTES)
                || currentToken.Is(SpiceTokenType.PERCENT))
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(currentToken.SpiceTokenType, current));
            }
            else
            {
                throw new ParseException("Error during parsing a paremeter. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.Model"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadModel(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];
            var nextToken = tokens[currentTokenIndex + 1];
            var nextNextToken = tokens[currentTokenIndex + 2];

            if (currentToken.Is(SpiceTokenType.DOT)
                && nextToken.Is(SpiceTokenType.WORD)
                && nextToken.Equal("model", true)
                && (nextNextToken.Is(SpiceTokenType.WORD) || nextNextToken.Is(SpiceTokenType.IDENTIFIER)))
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                    CreateTerminalNode(nextToken.SpiceTokenType, currentNode),
                    CreateTerminalNode(nextNextToken.SpiceTokenType, currentNode),
                    CreateNonTerminalNode(Symbols.Parameters, currentNode));
            }
            else
            {
                throw new ParseException("Error during parsing a model, line=" + currentToken.LineNumber, currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.Control"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadControl(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];
            var nextToken = tokens[currentTokenIndex + 1];

            if (currentToken.Is(SpiceTokenType.DOT) && nextToken.Is(SpiceTokenType.WORD))
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                    CreateTerminalNode(nextToken.SpiceTokenType, currentNode),
                    CreateNonTerminalNode(Symbols.Parameters, currentNode));
            }
            else
            {
                if (currentToken.Is(SpiceTokenType.ENDL))
                {
                    PushProductionExpression(
                        stack,
                        CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                        CreateNonTerminalNode(Symbols.Parameters, currentNode));
                }
                else if (currentToken.Is(SpiceTokenType.IF))
                {
                    PushProductionExpression(
                        stack,
                        CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                        CreateTerminalNode(SpiceTokenType.BOOLEAN_EXPRESSION, currentNode));
                }
                else if (currentToken.Is(SpiceTokenType.ELSE_IF))
                {
                    PushProductionExpression(
                        stack,
                        CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                        CreateTerminalNode(SpiceTokenType.BOOLEAN_EXPRESSION, currentNode));
                }
                else if (currentToken.Is(SpiceTokenType.ELSE))
                {
                    PushProductionExpression(
                        stack,
                        CreateTerminalNode(currentToken.SpiceTokenType, currentNode));
                }
                else if (currentToken.Is(SpiceTokenType.ENDIF))
                {
                    PushProductionExpression(
                        stack,
                        CreateTerminalNode(currentToken.SpiceTokenType, currentNode));
                }
                else
                {
                    throw new ParseException("Error during parsing a control. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
                }
            }
        }

        /// <summary>
        /// Reads <see cref="Symbols.Component"/> non-terminal node
        /// Pushes tree nodes to the stack based on the grammar.
        /// </summary>
        /// <param name="stack">A stack where the production is pushed</param>
        /// <param name="currentNode">A reference to the current node</param>
        /// <param name="tokens">A reference to the array of tokens</param>
        /// <param name="currentTokenIndex">A index of the current token</param>
        private void ReadComponent(Stack<ParseTreeNode> stack, ParseTreeNonTerminalNode currentNode, SpiceToken[] tokens, int currentTokenIndex)
        {
            var currentToken = tokens[currentTokenIndex];

            if (currentToken.Is(SpiceTokenType.WORD))
            {
                PushProductionExpression(
                    stack,
                    CreateTerminalNode(currentToken.SpiceTokenType, currentNode),
                    CreateNonTerminalNode(Symbols.Parameters, currentNode));
            }
            else
            {
                throw new ParseException("Error during parsing a component. Unexpected token: '" + currentToken.Lexem + "'" + " line=" + currentToken.LineNumber, currentToken.LineNumber);
            }
        }

        /// <summary>
        /// Creates a new non-terminal node
        /// </summary>
        /// <param name="symbolName">A name of non-terminal</param>
        /// <param name="parent">A parent of the new non-terminal node</param>
        /// <returns>
        /// A new instance of <see cref="ParseTreeNonTerminalNode"/>
        /// </returns>
        private ParseTreeNonTerminalNode CreateNonTerminalNode(string symbolName, ParseTreeNonTerminalNode parent)
        {
            if (parent == null)
            {
                return new ParseTreeNonTerminalNode(symbolName);
            }

            var node = new ParseTreeNonTerminalNode(parent, symbolName);
            parent.Children.Add(node);

            return node;
        }

        /// <summary>
        /// Creates a new terminal node
        /// </summary>
        /// <param name="tokenType">A type of the token</param>
        /// <param name="parent">A parent of the new terminal node</param>
        /// <param name="tokenValue">An expected lexem for the terminal node</param>
        /// <returns>
        /// A new instance of <see cref="ParseTreeTerminalNode"/>
        /// </returns>
        private ParseTreeTerminalNode CreateTerminalNode(SpiceTokenType tokenType, ParseTreeNonTerminalNode parent, string tokenValue = null)
        {
            var node = new ParseTreeTerminalNode(new SpiceToken(tokenType, tokenValue), parent);
            parent.Children.Add(node);
            return node;
        }

        /// <summary>
        /// Pushes grammar production expression to stack
        /// </summary>
        /// <param name="stack">A stack</param>
        /// <param name="expression">An expression of production</param>
        private void PushProductionExpression(Stack<ParseTreeNode> stack, params ParseTreeNode[] expression)
        {
            for (var i = expression.Length - 1; i >= 0; i--)
            {
                stack.Push(expression[i]);
            }
        }
    }
}

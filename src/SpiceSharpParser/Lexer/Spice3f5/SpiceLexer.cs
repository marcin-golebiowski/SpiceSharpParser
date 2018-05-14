﻿using SpiceSharpParser.Grammar;
using SpiceSharpParser.Lexer;
using System.Collections.Generic;

namespace SpiceSharpParser.Lexer.Spice3f5
{
    /// <summary>
    /// A lexer for Spice netlists
    /// </summary>
    public class SpiceLexer
    {
        private LexerGrammar<SpiceLexerState> grammar;
        private SpiceLexerOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpiceLexer"/> class.
        /// </summary>
        /// <param name="options">options for lexer</param>
        public SpiceLexer(SpiceLexerOptions options)
        {
            this.options = options ?? throw new System.ArgumentNullException(nameof(options));
            BuildGrammar();
        }

        /// <summary>
        /// Gets tokens for Spice netlist
        /// </summary>
        /// <param name="netlistText">A string with Spice netlist</param>
        /// <returns>
        /// An enumerable of tokens
        /// </returns>
        public IEnumerable<SpiceToken> GetTokens(string netlistText)
        {
            var state = new SpiceLexerState();
            var lexer = new Lexer<SpiceLexerState>(grammar, new LexerOptions(true, '+'));

            foreach (var token in lexer.GetTokens(netlistText, state))
            {
                yield return new SpiceToken((SpiceTokenType)token.TokenType, token.Lexem, state.LineNumber);
            }
        }

        /// <summary>
        /// Builds Spice lexer grammar
        /// </summary>
        private void BuildGrammar()
        {
            var builder = new LexerGrammarBuilder<SpiceLexerState>();
            builder.AddRule(new LexerInternalRule("LETTER", "[a-z]", options.IgnoreCase));
            builder.AddRule(new LexerInternalRule("CHARACTER", "[a-z0-9\\-+]", options.IgnoreCase));
            builder.AddRule(new LexerInternalRule("DIGIT", "[0-9]", options.IgnoreCase));
            builder.AddRule(new LexerInternalRule("SPECIAL", "[\\\\\\[\\]_\\.\\:\\!%\\#\\-;\\<>\\^+/\\*]", options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.WHITESPACE,
                "A whitespace characters that will be ignored",
                "[ \t]*",
                (SpiceLexerState state, string lexem) =>
                {
                    return LexerRuleResult.IgnoreToken;
                }));

            builder.AddRule(
                new LexerTokenRule<SpiceLexerState>(
                    (int)SpiceTokenType.DOT,
                    "A dot character",
                    "\\."));

            builder.AddRule(
                new LexerTokenRule<SpiceLexerState>(
                    (int)SpiceTokenType.COMMA,
                    "A comma character",
                    ","));

            builder.AddRule(
                new LexerTokenRule<SpiceLexerState>(
                    (int)SpiceTokenType.DELIMITER,
                    "A delimeter character",
                    @"(\(|\)|\|)",
                    (SpiceLexerState state, string lexem) =>
                     {
                         return LexerRuleResult.ReturnToken;
                     }));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.EQUAL,
                "An equal character",
                @"="));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.NEWLINE,
                "A new line characters",
                @"(\r\n|\n|\r)",
                (SpiceLexerState state, string lexem) =>
                {
                    state.LineNumber++;
                    return LexerRuleResult.ReturnToken;
                }));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.CONTINUE,
                "A continuation token",
                @"((\r\n\+|\n\+|\r\+))",
                (SpiceLexerState state, string lexem) =>
                {
                    state.LineNumber++;
                    return LexerRuleResult.IgnoreToken;
                }));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.ENDS,
                ".ends keyword",
                ".ends",
                ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.END,
                ".end keyword",
                ".end",
                ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
               (int)SpiceTokenType.ENDL_HSPICE,
               ".endl keyword",
               ".endl",
               ignoreCase: options.IgnoreCase));


            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
               (int)SpiceTokenType.VALUE,
               "A value with comma seperator",
               @"([+-]?((<DIGIT>)+(,(<DIGIT>)*)?|\.(<DIGIT>)+)(e(\+|-)?(<DIGIT>)+)?[tgmkunpf]?(<LETTER>)*)",
               null,
               (SpiceLexerState state) =>
               {
                   if (state.PreviousTokenType == (int)SpiceTokenType.EQUAL
                    || state.PreviousTokenType == (int)SpiceTokenType.VALUE)
                   {
                       return LexerRuleUseState.Use;
                   }

                   return LexerRuleUseState.Skip;
               },
               ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.VALUE,
                "A value with dot seperator",
                @"([+-]?((<DIGIT>)+(\.(<DIGIT>)*)?|\.(<DIGIT>)+)(e(\+|-)?(<DIGIT>)+)?[tgmkunpf]?(<LETTER>)*)",
                null,
                (SpiceLexerState state) =>
                {
                    return LexerRuleUseState.Use;
                },
                ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
             (int)SpiceTokenType.COMMENT_HSPICE,
             "A comment - HSpice style",
             @"\$[^\r\n]*",
             (SpiceLexerState state, string lexem) =>
             {
                 return LexerRuleResult.IgnoreToken;
             }));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
             (int)SpiceTokenType.COMMENT_PSPICE,
             "A comment - PSpice style",
             @";[^\r\n]*",
             (SpiceLexerState state, string lexem) =>
             {
                 return LexerRuleResult.IgnoreToken;
             }));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.COMMENT,
                "A full line comment",
                @"\*[^\r\n]*",
                null,
                (SpiceLexerState state) =>
                {
                    if (state.PreviousTokenType == (int)SpiceTokenType.NEWLINE
                    || (state.LineNumber == 1 && options.HasTitle == false))
                    {
                        return LexerRuleUseState.Use;
                    }
                    return LexerRuleUseState.Skip;
                },
                 ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.TITLE,
                "The title - first line of spice token",
                @"[^\r\n]+",
                null,
                (SpiceLexerState state) =>
                {
                    if (state.LineNumber == 1 && options.HasTitle)
                    {
                        return LexerRuleUseState.Use;
                    }
                    return LexerRuleUseState.Skip;
                }));

            builder.AddRule(
                new LexerTokenRule<SpiceLexerState>(
                    (int)SpiceTokenType.STRING,
                    "A string with quotation marks",
                    "\"(?:[^\"\\\\]|\\\\.)*\"",
                    ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.EXPRESSION,
                "A mathematical expression",
                "{[^{}]*}",
                ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.REFERENCE,
                "A reference",
                "@(<LETTER>(<CHARACTER>|<SPECIAL>)*)",
                ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
                (int)SpiceTokenType.WORD,
                "A word",
                "(<LETTER>(<CHARACTER>|<SPECIAL>)*)",
                ignoreCase: options.IgnoreCase));

            builder.AddRule(
                new LexerTokenRule<SpiceLexerState>(
                    (int)SpiceTokenType.IDENTIFIER,
                    "An identifier",
                    "((<CHARACTER>|_|\\*)(<CHARACTER>|<SPECIAL>)*)",
                    ignoreCase: options.IgnoreCase));

            builder.AddRule(new LexerTokenRule<SpiceLexerState>(
              (int)SpiceTokenType.ASTERIKS,
              "An asteriks character",
              "\\*"));

            grammar = builder.GetGrammar();
        }
    }
}

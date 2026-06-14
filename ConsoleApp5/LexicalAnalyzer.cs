using System;

namespace Компилятор
{
    public class LexicalAnalyzer
    {
        public const byte
            IDENT = 2,
            INTCONST = 15,
            REALCONST = 82,
            CHARCONST = 84,
            PLUS = 70,
            MINUS = 71,
            STAR = 21,
            SLASH = 60,
            EQUAL = 16,
            ASSIGN = 51,
            SEMICOLON = 14,
            COLON = 5,
            COMMA = 20,
            POINT = 61,
            DOTDOT = 74,
            LPAREN = 9,
            RPAREN = 4,
            LBRACKET = 11,
            RBRACKET = 12,
            LESS = 65,
            GREATER = 66,
            LESSEQUAL = 67,
            GREATEREQUAL = 68,
            NOTEQUAL = 69,
            CARET = 62;

        private const int MIN_INTEGER = 0;
        private const int MAX_INTEGER = 65535;

        private byte currentSymbol;
        private TextPosition tokenPosition;
        private string identifierName;
        private int intValue;
        private float floatValue;
        private char charValue;

        public LexicalAnalyzer()
        {
            currentSymbol = 0;
            tokenPosition = new TextPosition();
            identifierName = "";
            intValue = 0;
            floatValue = 0;
            charValue = '\0';
        }

        public byte NextToken()
        {
            // Пропускаем пробелы, табуляции, переводы строк
            while (true)
            {
                char ch = InputOutput.Ch;

                if (ch == '\0')
                {
                    currentSymbol = 0;
                    return 0;
                }

                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    InputOutput.NextCh();
                    continue;
                }

                break;
            }

            tokenPosition = InputOutput.positionNow;
            char currentChar = InputOutput.Ch;

            if (char.IsDigit(currentChar))
            {
                ScanNumber();
                return currentSymbol;
            }

            if (char.IsLetter(currentChar))
            {
                ScanIdentifierOrKeyword();
                return currentSymbol;
            }

            if (currentChar == '\'')
            {
                ScanCharConstant();
                return currentSymbol;
            }

            ScanOperator();
            return currentSymbol;
        }

        private void ScanNumber()
        {
            string numberStr = "";
            bool isReal = false;
            int startLine = (int)tokenPosition.LineNumber;
            byte startChar = tokenPosition.CharNumber;

            while (char.IsDigit(InputOutput.Ch))
            {
                numberStr += InputOutput.Ch;
                InputOutput.NextCh();
                if (InputOutput.Ch == '\0')
                {
                    break;
                }
            }

            if (InputOutput.Ch == '.')
            {
                InputOutput.NextCh();

                if (char.IsDigit(InputOutput.Ch))
                {
                    isReal = true;
                    numberStr += '.';

                    while (char.IsDigit(InputOutput.Ch))
                    {
                        numberStr += InputOutput.Ch;
                        InputOutput.NextCh();
                        if (InputOutput.Ch == '\0') break;
                    }
                }
                else
                {
                    InputOutput.DecrementCharNumber();
                }
            }

            if (!isReal && (InputOutput.Ch == 'E' || InputOutput.Ch == 'e'))
            {
                isReal = true;
                numberStr += InputOutput.Ch;
                InputOutput.NextCh();

                if (InputOutput.Ch == '+' || InputOutput.Ch == '-')
                {
                    numberStr += InputOutput.Ch;
                    InputOutput.NextCh();
                }

                while (char.IsDigit(InputOutput.Ch))
                {
                    numberStr += InputOutput.Ch;
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '\0') break;
                }
            }

            if (!isReal)
            {
                if (int.TryParse(numberStr, out intValue))
                {
                    if (intValue < MIN_INTEGER || intValue > MAX_INTEGER)
                        InputOutput.Error(203, new TextPosition((uint)startLine, startChar));

                    currentSymbol = INTCONST;
                }
                else
                {
                    InputOutput.Error(43, InputOutput.positionNow);
                    currentSymbol = 0;
                }
            }
            else
            {
                if (float.TryParse(numberStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out floatValue))
                {
                    currentSymbol = REALCONST;
                }
                else
                {
                    InputOutput.Error(44, InputOutput.positionNow);
                    currentSymbol = 0;
                }
            }
        }

        private void ScanIdentifierOrKeyword()
        {
            string name = "";

            while (char.IsLetterOrDigit(InputOutput.Ch))
            {
                name += InputOutput.Ch;
                InputOutput.NextCh();
                if (InputOutput.Ch == '\0') break;
            }

            if (Keywords.IsKeyword(name, out byte keywordCode))
            {
                currentSymbol = keywordCode;
                identifierName = name;
            }
            else
            {
                currentSymbol = IDENT;
                identifierName = name;
            }
        }

        private void ScanCharConstant()
        {
            InputOutput.NextCh();

            if (InputOutput.Ch == '\'')
            {
                InputOutput.Error(204, InputOutput.positionNow);
                charValue = ' ';
            }
            else
            {
                charValue = InputOutput.Ch;
                InputOutput.NextCh();

                if (InputOutput.Ch != '\'')
                    InputOutput.Error(205, InputOutput.positionNow);
                else
                    InputOutput.NextCh();
            }

            currentSymbol = CHARCONST;
        }

        private void ScanOperator()
        {
            char ch = InputOutput.Ch;

            // Защита от пустого символа
            if (ch == '\0')
            {
                currentSymbol = 0;
                return;
            }

            switch (ch)
            {
                case '+':
                    currentSymbol = PLUS;
                    InputOutput.NextCh();
                    break;
                case '-':
                    currentSymbol = MINUS;
                    InputOutput.NextCh();
                    break;
                case '*':
                    currentSymbol = STAR;
                    InputOutput.NextCh();
                    break;
                case '/':
                    currentSymbol = SLASH;
                    InputOutput.NextCh();
                    break;
                case '=':
                    currentSymbol = EQUAL;
                    InputOutput.NextCh();
                    break;
                case ';':
                    currentSymbol = SEMICOLON;
                    InputOutput.NextCh();
                    break;
                case ',':
                    currentSymbol = COMMA;
                    InputOutput.NextCh();
                    break;
                case '.':
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '.')
                    {
                        currentSymbol = DOTDOT;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        currentSymbol = POINT;
                    }
                    break;
                case ':':
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '=')
                    {
                        currentSymbol = ASSIGN;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        currentSymbol = COLON;
                    }
                    break;
                case '<':
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '=')
                    {
                        currentSymbol = LESSEQUAL;
                        InputOutput.NextCh();
                    }
                    else if (InputOutput.Ch == '>')
                    {
                        currentSymbol = NOTEQUAL;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        currentSymbol = LESS;
                    }
                    break;
                case '>':
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '=')
                    {
                        currentSymbol = GREATEREQUAL;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        currentSymbol = GREATER;
                    }
                    break;
                case '(':
                    currentSymbol = LPAREN;
                    InputOutput.NextCh();
                    break;
                case ')':
                    currentSymbol = RPAREN;
                    InputOutput.NextCh();
                    break;
                case '[':
                    currentSymbol = LBRACKET;
                    InputOutput.NextCh();
                    break;
                case ']':
                    currentSymbol = RBRACKET;
                    InputOutput.NextCh();
                    break;
                case '^':
                    currentSymbol = CARET;
                    InputOutput.NextCh();
                    break;
                default:
                    InputOutput.Error(1, InputOutput.positionNow);
                    InputOutput.NextCh();
                    currentSymbol = 0;
                    break;
            }
        }

        public string GetTokenName(byte code)
        {
            switch (code)
            {
                case IDENT: return "IDENTIFIER";
                case INTCONST: return "INTEGER";
                case REALCONST: return "REAL";
                case CHARCONST: return "CHAR";
                case PLUS: return "PLUS";
                case MINUS: return "MINUS";
                case STAR: return "STAR";
                case SLASH: return "SLASH";
                case EQUAL: return "EQUAL";
                case ASSIGN: return "ASSIGN";
                case SEMICOLON: return "SEMICOLON";
                case COLON: return "COLON";
                case DOTDOT: return "DOTDOT";
                case POINT: return "POINT";
                case LESS: return "LESS";
                case GREATER: return "GREATER";
                case LESSEQUAL: return "LESS_EQUAL";
                case GREATEREQUAL: return "GREATER_EQUAL";
                case NOTEQUAL: return "NOT_EQUAL";
                case LPAREN: return "LEFT_PAREN";
                case RPAREN: return "RIGHT_PAREN";
                case LBRACKET: return "LEFT_BRACKET";
                case RBRACKET: return "RIGHT_BRACKET";
                case CARET: return "CARET";
                default:
                    foreach (var kw in Keywords.GetKeywordTable())
                        if (kw.Value == code) return kw.Key.ToUpper();
                    return $"UNKNOWN_{code}";
            }
        }

        public byte CurrentSymbol => currentSymbol;
        public TextPosition TokenPosition => tokenPosition;
        public string IdentifierName => identifierName;
        public int IntValue => intValue;
        public float FloatValue => floatValue;
        public char CharValue => charValue;
        public bool IsIdentifier => currentSymbol == IDENT;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Компилятор
{
    public class SyntaxAnalyzer
    {
        private LexicalAnalyzer lexer;
        private byte currentToken;
        private bool hasError;
        private StringBuilder errorLog;

        // Таблица символов для хранения описанных переменных и типов
        private Dictionary<string, SymbolInfo> symbolTable;
        private Dictionary<string, TypeInfo> typeTable;

        public SyntaxAnalyzer(LexicalAnalyzer lexicalAnalyzer)
        {
            lexer = lexicalAnalyzer;
            currentToken = 0;
            hasError = false;
            errorLog = new StringBuilder();
            symbolTable = new Dictionary<string, SymbolInfo>();
            typeTable = new Dictionary<string, TypeInfo>();
        }

        // Основной метод запуска синтаксического анализа
        public bool ParseProgram()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("СИНТАКСИЧЕСКИЙ АНАЛИЗАТОР");
            Console.WriteLine("========================================\n");

            hasError = false;
            errorLog.Clear();
            symbolTable.Clear();
            typeTable.Clear();

            NextToken();

            // PROGRAM IDENTIFIER ;
            if (currentToken == 29) // PROGRAM
            {
                Console.WriteLine("DEBUG: Найдено PROGRAM");
                NextToken();
                if (currentToken == LexicalAnalyzer.IDENT)
                {
                    Console.WriteLine($"DEBUG: Имя программы: {lexer.IdentifierName}");
                    NextToken();
                    if (currentToken == LexicalAnalyzer.SEMICOLON)
                    {
                        NextToken();
                    }
                    else
                    {
                        Error(3); // Ожидалось ';'
                        SkipTo(LexicalAnalyzer.SEMICOLON);
                        if (currentToken == LexicalAnalyzer.SEMICOLON) NextToken();
                    }
                }
                else
                {
                    Error(2); // Ожидался идентификатор
                    SkipTo(LexicalAnalyzer.SEMICOLON);
                    if (currentToken == LexicalAnalyzer.SEMICOLON) NextToken();
                }
            }

            // Блок описаний (VAR, CONST, TYPE, etc.)
            ParseDeclarationBlock();

            // BEGIN ... END.
            if (currentToken == 13) // BEGIN
            {
                Console.WriteLine("DEBUG: Найдено BEGIN");
                ParseCompoundStatement();
                if (currentToken == LexicalAnalyzer.POINT)
                {
                    Console.WriteLine("DEBUG: Найдена точка в конце программы");
                }
                else
                {
                    Error(10); // Ожидалось '.'
                }
            }
            else
            {
                Error(13); // Ожидалось BEGIN
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine("ТАБЛИЦА СИМВОЛОВ");
            Console.WriteLine("========================================");
            PrintSymbolTable();

            Console.WriteLine("\n========================================");
            if (!hasError)
                Console.WriteLine("СИНТАКСИЧЕСКИЙ АНАЛИЗ ЗАВЕРШЕН УСПЕШНО");
            else
                Console.WriteLine("СИНТАКСИЧЕСКИЙ АНАЛИЗ ЗАВЕРШЕН С ОШИБКАМИ");
            Console.WriteLine("========================================");

            if (hasError)
            {
                Console.WriteLine("\nСписок ошибок:");
                Console.WriteLine(errorLog.ToString());
            }

            return !hasError;
        }

        // Получение следующего токена
        private void NextToken()
        {
            currentToken = lexer.NextToken();
            Console.WriteLine($"DEBUG: Токен {currentToken} - {lexer.GetTokenName(currentToken)}");
        }

        // Пропуск токенов до указанного
        private void SkipTo(byte targetToken)
        {
            while (currentToken != 0 && currentToken != targetToken)
            {
                NextToken();
            }
        }

        // Пропуск токенов до одного из указанных
        private void SkipTo(params byte[] targetTokens)
        {
            while (currentToken != 0)
            {
                foreach (byte target in targetTokens)
                {
                    if (currentToken == target) return;
                }
                NextToken();
            }
        }

        // Обработка ошибок с нейтрализацией
        private void Error(byte errorCode)
        {
            hasError = true;
            string message = Errors.GetMessage(errorCode);
            string position = $"стр.{lexer.TokenPosition.LineNumber}, поз.{lexer.TokenPosition.CharNumber}";
            errorLog.AppendLine($"ОШИБКА {errorCode}: {message} ({position})");
            Console.WriteLine($"DEBUG: ОШИБКА {errorCode}: {message} ({position})");
        }

        // Парсинг блока описаний
        private void ParseDeclarationBlock()
        {
            while (currentToken == 32 || currentToken == 34 || currentToken == 33 ||
                   currentToken == 35 || currentToken == 30 || currentToken == 31)
            {
                switch (currentToken)
                {
                    case 32: // VAR
                        ParseVariableDeclaration();
                        break;
                    case 33: // TYPE
                        ParseTypeDeclaration();
                        break;
                    case 34: // CONST
                        ParseConstDeclaration();
                        break;
                    default:
                        NextToken(); // Пропускаем неизвестные объявления
                        break;
                }
            }
        }

        // Парсинг описания переменных
        private void ParseVariableDeclaration()
        {
            Console.WriteLine("DEBUG: Парсинг VAR секции");
            NextToken(); // Пропускаем VAR

            while (currentToken == LexicalAnalyzer.IDENT)
            {
                ParseVariableList();
            }
        }

        // Парсинг списка переменных
        private void ParseVariableList()
        {
            List<string> identifiers = new List<string>();

            // Собираем список идентификаторов через запятую
            while (currentToken == LexicalAnalyzer.IDENT)
            {
                identifiers.Add(lexer.IdentifierName);
                Console.WriteLine($"DEBUG: Добавлен идентификатор: {lexer.IdentifierName}");
                NextToken();

                if (currentToken == LexicalAnalyzer.COMMA)
                {
                    NextToken();
                }
                else if (currentToken == LexicalAnalyzer.COLON)
                {
                    break;
                }
                else
                {
                    Error(4); // Ожидалось ':'
                    SkipTo(LexicalAnalyzer.COLON, LexicalAnalyzer.SEMICOLON);
                    if (currentToken != LexicalAnalyzer.COLON) return;
                    break;
                }
            }

            // Должно быть ':'
            if (currentToken == LexicalAnalyzer.COLON)
            {
                NextToken();
            }
            else
            {
                Error(4); // Ожидалось ':'
                return;
            }

            // Парсим тип
            TypeInfo typeInfo = ParseType();

            if (typeInfo != null)
            {
                // Добавляем переменные в таблицу символов
                foreach (string id in identifiers)
                {
                    if (!symbolTable.ContainsKey(id.ToLower()))
                    {
                        symbolTable.Add(id.ToLower(), new SymbolInfo
                        {
                            Name = id,
                            Type = typeInfo,
                            Kind = SymbolKind.Variable
                        });
                        Console.WriteLine($"DEBUG: Добавлена переменная '{id}' типа '{typeInfo.Name}'");
                    }
                    else
                    {
                        Console.WriteLine($"DEBUG: Переменная '{id}' уже существует");
                    }
                }
            }

            // Ожидаем ';' или следующее описание
            if (currentToken == LexicalAnalyzer.SEMICOLON)
            {
                NextToken();
            }
            else if (currentToken != LexicalAnalyzer.IDENT &&
                     currentToken != 13 && currentToken != 32 && currentToken != 34 &&
                     currentToken != 33 && currentToken != 30 && currentToken != 31)
            {
                Error(3); // Ожидалось ';'
                SkipTo(LexicalAnalyzer.SEMICOLON);
                if (currentToken == LexicalAnalyzer.SEMICOLON) NextToken();
            }
        }

        // Парсинг типа
        private TypeInfo ParseType()
        {
            TypeInfo typeInfo = null;

            Console.WriteLine($"DEBUG: Парсинг типа, текущий токен: {currentToken}");

            // Стандартные типы
            if (currentToken == LexicalAnalyzer.IDENT)
            {
                string typeName = lexer.IdentifierName.ToLower();
                Console.WriteLine($"DEBUG: Найден тип: {typeName}");

                // Проверяем стандартные типы
                if (typeName == "integer" || typeName == "real" || typeName == "boolean" ||
                    typeName == "char" || typeName == "string")
                {
                    typeInfo = new TypeInfo { Name = typeName, Kind = TypeKind.Standard };
                    NextToken();
                }
                // Проверяем пользовательские типы
                else if (typeTable.ContainsKey(typeName))
                {
                    typeInfo = typeTable[typeName];
                    NextToken();
                }
                else
                {
                    Error(60); // Тип не определён
                    NextToken();
                }
            }
            // Запись (RECORD)
            else if (currentToken == 38) // RECORD
            {
                typeInfo = ParseRecordType();
            }
            // Массив (ARRAY)
            else if (currentToken == 36) // ARRAY
            {
                typeInfo = ParseArrayType();
            }
            else
            {
                Error(60); // Тип не определён
            }

            return typeInfo;
        }

        // Парсинг типа RECORD
        private TypeInfo ParseRecordType()
        {
            Console.WriteLine("DEBUG: Парсинг RECORD типа");
            NextToken(); // Пропускаем RECORD

            TypeInfo recordType = new TypeInfo
            {
                Name = "record",
                Kind = TypeKind.Record,
                Fields = new Dictionary<string, TypeInfo>()
            };

            // Парсим поля записи
            while (currentToken == LexicalAnalyzer.IDENT)
            {
                ParseRecordField(recordType);

                if (currentToken == LexicalAnalyzer.SEMICOLON)
                {
                    NextToken();
                }
                else if (currentToken == 14) // END
                {
                    break;
                }
                else
                {
                    Error(3); // Ожидалось ';'
                    SkipTo(LexicalAnalyzer.SEMICOLON, 14);
                    if (currentToken == LexicalAnalyzer.SEMICOLON) NextToken();
                }
            }

            // Ожидаем END
            if (currentToken == 14) // END
            {
                NextToken();
            }
            else
            {
                Error(14); // Ожидалось END
            }

            return recordType;
        }

        // Парсинг поля записи
        private void ParseRecordField(TypeInfo recordType)
        {
            List<string> fieldNames = new List<string>();

            // Собираем имена полей
            while (currentToken == LexicalAnalyzer.IDENT)
            {
                fieldNames.Add(lexer.IdentifierName);
                Console.WriteLine($"DEBUG: Поле записи: {lexer.IdentifierName}");
                NextToken();

                if (currentToken == LexicalAnalyzer.COMMA)
                {
                    NextToken();
                }
                else if (currentToken == LexicalAnalyzer.COLON)
                {
                    break;
                }
                else
                {
                    Error(4); // Ожидалось ':'
                    SkipTo(LexicalAnalyzer.COLON, LexicalAnalyzer.SEMICOLON, 14);
                    if (currentToken != LexicalAnalyzer.COLON) return;
                    break;
                }
            }

            // Ожидаем ':'
            if (currentToken == LexicalAnalyzer.COLON)
            {
                NextToken();
            }
            else
            {
                Error(4); // Ожидалось ':'
                return;
            }

            // Парсим тип поля
            TypeInfo fieldType = ParseType();

            if (fieldType != null)
            {
                foreach (string fieldName in fieldNames)
                {
                    if (!recordType.Fields.ContainsKey(fieldName.ToLower()))
                    {
                        recordType.Fields.Add(fieldName.ToLower(), fieldType);
                        Console.WriteLine($"DEBUG: Добавлено поле '{fieldName}' типа '{fieldType.Name}'");
                    }
                }
            }
        }

        // Парсинг типа ARRAY
        private TypeInfo ParseArrayType()
        {
            Console.WriteLine("DEBUG: Парсинг ARRAY типа");
            NextToken(); // Пропускаем ARRAY

            // Ожидаем '['
            if (currentToken == LexicalAnalyzer.LBRACKET)
            {
                NextToken();
            }
            else
            {
                Error(8); // Ожидалось '['
                return null;
            }

            // Парсим диапазон индексов
            ParseSimpleExpression(); // Нижняя граница

            if (currentToken == LexicalAnalyzer.DOTDOT) // ..
            {
                NextToken();
                ParseSimpleExpression(); // Верхняя граница
            }
            else
            {
                Error(11); // Ожидалось '..'
            }

            // Ожидаем ']'
            if (currentToken == LexicalAnalyzer.RBRACKET)
            {
                NextToken();
            }
            else
            {
                Error(9); // Ожидалось ']'
                SkipTo(LexicalAnalyzer.RBRACKET);
                if (currentToken == LexicalAnalyzer.RBRACKET) NextToken();
            }

            // Ожидаем OF
            if (currentToken == 24) // OF
            {
                NextToken();
            }
            else
            {
                Error(24); // Ожидалось OF
                return null;
            }

            // Парсим тип элементов
            TypeInfo elementType = ParseType();

            return new TypeInfo
            {
                Name = "array",
                Kind = TypeKind.Array,
                ElementType = elementType
            };
        }

        // Парсинг описания типов
        private void ParseTypeDeclaration()
        {
            Console.WriteLine("DEBUG: Парсинг TYPE секции");
            NextToken(); // Пропускаем TYPE

            while (currentToken == LexicalAnalyzer.IDENT)
            {
                string typeName = lexer.IdentifierName;
                Console.WriteLine($"DEBUG: Новый тип: {typeName}");
                NextToken();

                if (currentToken == LexicalAnalyzer.EQUAL) // =
                {
                    NextToken();
                    TypeInfo typeInfo = ParseType();

                    if (typeInfo != null && !typeTable.ContainsKey(typeName.ToLower()))
                    {
                        typeInfo.Name = typeName;
                        typeTable.Add(typeName.ToLower(), typeInfo);
                        Console.WriteLine($"DEBUG: Добавлен тип '{typeName}'");
                    }
                }
                else
                {
                    Error(5); // Ожидалось '='
                    SkipTo(LexicalAnalyzer.SEMICOLON);
                }

                if (currentToken == LexicalAnalyzer.SEMICOLON)
                {
                    NextToken();
                }
                else if (currentToken != LexicalAnalyzer.IDENT && currentToken != 32 && currentToken != 13)
                {
                    Error(3); // Ожидалось ';'
                }
            }
        }

        // Парсинг описания констант
        private void ParseConstDeclaration()
        {
            Console.WriteLine("DEBUG: Парсинг CONST секции");
            NextToken(); // Пропускаем CONST

            while (currentToken == LexicalAnalyzer.IDENT)
            {
                string constName = lexer.IdentifierName;
                Console.WriteLine($"DEBUG: Константа: {constName}");
                NextToken();

                if (currentToken == LexicalAnalyzer.EQUAL) // =
                {
                    NextToken();
                    ParseConstValue();
                }
                else
                {
                    Error(5); // Ожидалось '='
                }

                if (currentToken == LexicalAnalyzer.SEMICOLON)
                {
                    NextToken();
                }
                else
                {
                    Error(3); // Ожидалось ';'
                    SkipTo(LexicalAnalyzer.SEMICOLON);
                    if (currentToken == LexicalAnalyzer.SEMICOLON) NextToken();
                }
            }
        }

        // Парсинг значения константы
        private void ParseConstValue()
        {
            if (currentToken == LexicalAnalyzer.INTCONST ||
                currentToken == LexicalAnalyzer.REALCONST ||
                currentToken == LexicalAnalyzer.CHARCONST)
            {
                NextToken();
            }
            else if (currentToken == LexicalAnalyzer.IDENT)
            {
                NextToken();
            }
            else
            {
                Error(42); // Ожидалась константа
            }
        }

        // Парсинг составного оператора (BEGIN ... END)
        private void ParseCompoundStatement()
        {
            Console.WriteLine("DEBUG: Парсинг составного оператора");

            if (currentToken == 13) // BEGIN
            {
                NextToken();
            }

            // Парсим последовательность операторов
            while (currentToken != 14 && currentToken != 0) // Пока не END
            {
                ParseStatement();

                if (currentToken == LexicalAnalyzer.SEMICOLON)
                {
                    NextToken();
                }
                else if (currentToken != 14) // Если не END
                {
                    Error(3); // Ожидалось ';'
                    // Пытаемся восстановиться
                    SkipTo(LexicalAnalyzer.SEMICOLON, 14);
                    if (currentToken == LexicalAnalyzer.SEMICOLON) NextToken();
                }
            }

            // Ожидаем END
            if (currentToken == 14) // END
            {
                Console.WriteLine("DEBUG: Найдено END");
                NextToken();
            }
            else
            {
                Error(14); // Ожидалось END
            }
        }

        // Парсинг оператора
        private void ParseStatement()
        {
            Console.WriteLine($"DEBUG: Парсинг оператора, токен: {currentToken}");

            if (currentToken == LexicalAnalyzer.IDENT)
            {
                // Может быть присваивание или вызов процедуры
                string identifier = lexer.IdentifierName;
                Console.WriteLine($"DEBUG: Идентификатор в операторе: {identifier}");
                NextToken();

                if (currentToken == LexicalAnalyzer.ASSIGN) // :=
                {
                    ParseAssignmentStatement(identifier);
                }
                else if (currentToken == LexicalAnalyzer.POINT) // .
                {
                    // Доступ к полю записи
                    ParseRecordFieldAccess(identifier);

                    if (currentToken == LexicalAnalyzer.ASSIGN)
                    {
                        ParseAssignmentStatement(identifier);
                    }
                }
                else
                {
                    // Неизвестный оператор
                    Error(47); // Ожидался оператор
                }
            }
            else if (currentToken == 13) // BEGIN
            {
                ParseCompoundStatement();
            }
            else if (currentToken == 27) // WITH
            {
                ParseWithStatement();
            }
            else if (currentToken == 15) // IF
            {
                ParseIfStatement();
            }
            else if (currentToken == 18) // WHILE
            {
                ParseWhileStatement();
            }
            else if (currentToken == 20) // FOR
            {
                ParseForStatement();
            }
            else if (currentToken == 25) // REPEAT
            {
                ParseRepeatStatement();
            }
            else if (currentToken == 14) // END
            {
                // Конец составного оператора
                return;
            }
            else
            {
                Error(47); // Ожидался оператор
                NextToken();
            }
        }

        // Парсинг оператора присваивания
        private void ParseAssignmentStatement(string variableName)
        {
            Console.WriteLine($"DEBUG: Парсинг присваивания для '{variableName}'");

            // Проверяем, что переменная существует
            if (!symbolTable.ContainsKey(variableName.ToLower()))
            {
                Error(63); // Переменная не определена
            }

            // := уже прочитан
            NextToken();

            // Парсим выражение
            ParseExpression();
        }

        // Парсинг доступа к полю записи
        private void ParseRecordFieldAccess(string recordName)
        {
            Console.WriteLine($"DEBUG: Парсинг доступа к полю записи '{recordName}'");

            // Проверяем, что переменная существует
            if (!symbolTable.ContainsKey(recordName.ToLower()))
            {
                Error(63); // Переменная не определена
            }

            // . уже прочитан
            NextToken();

            // Ожидаем имя поля
            if (currentToken == LexicalAnalyzer.IDENT)
            {
                string fieldName = lexer.IdentifierName;
                Console.WriteLine($"DEBUG: Поле записи: {fieldName}");

                // Проверяем, что поле существует
                if (symbolTable.ContainsKey(recordName.ToLower()))
                {
                    var symbolInfo = symbolTable[recordName.ToLower()];
                    if (symbolInfo.Type.Kind == TypeKind.Record)
                    {
                        if (!symbolInfo.Type.Fields.ContainsKey(fieldName.ToLower()))
                        {
                            Console.WriteLine($"DEBUG: Поле '{fieldName}' не найдено в записи");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"DEBUG: '{recordName}' не является записью");
                    }
                }

                NextToken();

                // Проверяем вложенные поля
                if (currentToken == LexicalAnalyzer.POINT)
                {
                    ParseRecordFieldAccess(fieldName);
                }
            }
            else
            {
                Error(70); // Неверное имя поля
            }
        }

        // Парсинг оператора WITH
        private void ParseWithStatement()
        {
            Console.WriteLine("DEBUG: Парсинг оператора WITH");
            NextToken(); // Пропускаем WITH

            // Парсим список переменных записи
            ParseWithVariableList();

            // Ожидаем DO
            if (currentToken == 19) // DO
            {
                NextToken();
            }
            else
            {
                Error(19); // Ожидалось DO
            }

            // Парсим оператор
            ParseStatement();
        }

        // Парсинг списка переменных в WITH
        private void ParseWithVariableList()
        {
            while (currentToken == LexicalAnalyzer.IDENT)
            {
                string varName = lexer.IdentifierName;
                Console.WriteLine($"DEBUG: Переменная в WITH: {varName}");
                NextToken();

                if (currentToken == LexicalAnalyzer.COMMA)
                {
                    NextToken();
                }
                else if (currentToken == 19) // DO
                {
                    break;
                }
                else
                {
                    Error(19); // Ожидалось DO
                    break;
                }
            }
        }

        // Парсинг выражения
        private void ParseExpression()
        {
            Console.WriteLine("DEBUG: Парсинг выражения");
            ParseSimpleExpression();

            // Операторы сравнения
            if (currentToken == LexicalAnalyzer.EQUAL ||
                currentToken == LexicalAnalyzer.LESS ||
                currentToken == LexicalAnalyzer.GREATER ||
                currentToken == LexicalAnalyzer.LESSEQUAL ||
                currentToken == LexicalAnalyzer.GREATEREQUAL ||
                currentToken == LexicalAnalyzer.NOTEQUAL)
            {
                Console.WriteLine($"DEBUG: Оператор сравнения: {currentToken}");
                NextToken();
                ParseSimpleExpression();
            }
        }

        // Парсинг простого выражения
        private void ParseSimpleExpression()
        {
            // Знак + или - в начале
            if (currentToken == LexicalAnalyzer.PLUS || currentToken == LexicalAnalyzer.MINUS)
            {
                Console.WriteLine($"DEBUG: Унарный знак: {currentToken}");
                NextToken();
            }

            ParseTerm();

            // Операторы +, -
            while (currentToken == LexicalAnalyzer.PLUS ||
                   currentToken == LexicalAnalyzer.MINUS)
            {
                Console.WriteLine($"DEBUG: Оператор сложения: {currentToken}");
                NextToken();
                ParseTerm();
            }
        }

        // Парсинг терма
        private void ParseTerm()
        {
            ParseFactor();

            // Операторы *, /, DIV, MOD, AND
            while (currentToken == LexicalAnalyzer.STAR ||
                   currentToken == LexicalAnalyzer.SLASH ||
                   currentToken == 106 || // DIV
                   currentToken == 110 || // MOD
                   currentToken == 107)   // AND
            {
                Console.WriteLine($"DEBUG: Оператор умножения: {currentToken}");
                NextToken();
                ParseFactor();
            }
        }

        // Парсинг фактора
        private void ParseFactor()
        {
            Console.WriteLine($"DEBUG: Парсинг фактора, токен: {currentToken}");

            if (currentToken == LexicalAnalyzer.IDENT)
            {
                string identifier = lexer.IdentifierName;
                Console.WriteLine($"DEBUG: Идентификатор в выражении: {identifier}");
                NextToken();

                // Проверяем доступ к полю записи
                if (currentToken == LexicalAnalyzer.POINT)
                {
                    NextToken();
                    if (currentToken == LexicalAnalyzer.IDENT)
                    {
                        Console.WriteLine($"DEBUG: Поле в выражении: {lexer.IdentifierName}");
                        NextToken();
                    }
                    else
                    {
                        Error(70); // Неверное имя поля
                    }
                }
                // Проверяем индексацию массива
                else if (currentToken == LexicalAnalyzer.LBRACKET)
                {
                    NextToken();
                    ParseExpression();
                    if (currentToken == LexicalAnalyzer.RBRACKET)
                    {
                        NextToken();
                    }
                    else
                    {
                        Error(9); // Ожидалось ']'
                    }
                }
            }
            else if (currentToken == LexicalAnalyzer.INTCONST)
            {
                Console.WriteLine($"DEBUG: Целая константа: {lexer.IntValue}");
                NextToken();
            }
            else if (currentToken == LexicalAnalyzer.REALCONST)
            {
                Console.WriteLine($"DEBUG: Вещественная константа: {lexer.FloatValue}");
                NextToken();
            }
            else if (currentToken == LexicalAnalyzer.CHARCONST)
            {
                Console.WriteLine($"DEBUG: Символьная константа: {lexer.CharValue}");
                NextToken();
            }
            else if (currentToken == LexicalAnalyzer.LPAREN)
            {
                Console.WriteLine("DEBUG: Открывающая скобка");
                NextToken();
                ParseExpression();
                if (currentToken == LexicalAnalyzer.RPAREN)
                {
                    Console.WriteLine("DEBUG: Закрывающая скобка");
                    NextToken();
                }
                else
                {
                    Error(7); // Ожидалось ')'
                }
            }
            else if (currentToken == 108) // NOT
            {
                Console.WriteLine("DEBUG: Оператор NOT");
                NextToken();
                ParseFactor();
            }
            else
            {
                Error(48); // Ожидалось выражение
            }
        }

        // Парсинг IF оператора
        private void ParseIfStatement()
        {
            Console.WriteLine("DEBUG: Парсинг IF оператора");
            NextToken(); // Пропускаем IF

            ParseExpression();

            if (currentToken == 16) // THEN
            {
                NextToken();
            }
            else
            {
                Error(16); // Ожидалось THEN
            }

            ParseStatement();

            if (currentToken == 17) // ELSE
            {
                NextToken();
                ParseStatement();
            }
        }

        // Парсинг WHILE оператора
        private void ParseWhileStatement()
        {
            Console.WriteLine("DEBUG: Парсинг WHILE оператора");
            NextToken(); // Пропускаем WHILE

            ParseExpression();

            if (currentToken == 19) // DO
            {
                NextToken();
            }
            else
            {
                Error(19); // Ожидалось DO
            }

            ParseStatement();
        }

        // Парсинг FOR оператора
        private void ParseForStatement()
        {
            Console.WriteLine("DEBUG: Парсинг FOR оператора");
            NextToken(); // Пропускаем FOR

            if (currentToken == LexicalAnalyzer.IDENT)
            {
                NextToken();
            }
            else
            {
                Error(2); // Ожидался идентификатор
            }

            if (currentToken == LexicalAnalyzer.ASSIGN) // :=
            {
                NextToken();
            }
            else
            {
                Error(12); // Ожидалось ':='
            }

            ParseExpression();

            if (currentToken == 21) // TO
            {
                NextToken();
            }
            else if (currentToken == 22) // DOWNTO
            {
                NextToken();
            }
            else
            {
                Error(21); // Ожидалось TO
            }

            ParseExpression();

            if (currentToken == 19) // DO
            {
                NextToken();
            }
            else
            {
                Error(19); // Ожидалось DO
            }

            ParseStatement();
        }

        // Парсинг REPEAT оператора
        private void ParseRepeatStatement()
        {
            Console.WriteLine("DEBUG: Парсинг REPEAT оператора");
            NextToken(); // Пропускаем REPEAT

            // Парсим последовательность операторов
            while (currentToken != 26 && currentToken != 0) // Пока не UNTIL
            {
                ParseStatement();

                if (currentToken == LexicalAnalyzer.SEMICOLON)
                {
                    NextToken();
                }
                else if (currentToken != 26) // Если не UNTIL
                {
                    Error(3); // Ожидалось ';'
                }
            }

            if (currentToken == 26) // UNTIL
            {
                NextToken();
                ParseExpression();
            }
            else
            {
                Error(26); // Ожидалось UNTIL
            }
        }

        // Вывод таблицы символов
        private void PrintSymbolTable()
        {
            if (symbolTable.Count == 0)
            {
                Console.WriteLine("Таблица символов пуста");
                return;
            }

            Console.WriteLine($"{"ИМЯ",-20} | {"ТИП",-15} | {"КАТЕГОРИЯ",-10}");
            Console.WriteLine(new string('-', 50));

            foreach (var symbol in symbolTable.Values)
            {
                string kind = symbol.Kind == SymbolKind.Variable ? "Переменная" : "Тип";
                Console.WriteLine($"{symbol.Name,-20} | {symbol.Type.Name,-15} | {kind,-10}");
            }
        }
    }

    // Вспомогательные классы для таблицы символов
    public enum SymbolKind
    {
        Variable,
        Type
    }

    public enum TypeKind
    {
        Standard,
        Record,
        Array,
        UserDefined
    }

    public class SymbolInfo
    {
        public string Name { get; set; }
        public TypeInfo Type { get; set; }
        public SymbolKind Kind { get; set; }
    }

    public class TypeInfo
    {
        public string Name { get; set; }
        public TypeKind Kind { get; set; }
        public Dictionary<string, TypeInfo> Fields { get; set; } // Для записей
        public TypeInfo ElementType { get; set; } // Для массивов
    }
}

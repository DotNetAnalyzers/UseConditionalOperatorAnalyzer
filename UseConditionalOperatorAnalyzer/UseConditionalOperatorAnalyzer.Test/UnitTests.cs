using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using UseConditionalOperatorAnalyzer;

namespace UseConditionalOperatorAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [TestMethod]
        public void TestEmptyTreeHasNoDiagnostics()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestIfOnlyHasNoDiagnostics()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string GetMessage()
            {
                var message = default(string);
                var now = DateTime.Now;
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                    message = ""Weekend!"";
                return message;
            }
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestConditionalAssignmentAndReturnHasNoDiagnostics()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string GetMessage()
            {
                var message = default(string);
                var now = DateTime.Now;
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                    message = ""Weekend!"";
                else
                    return ""Weekday"";
                return message;
            }
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestConditionalFieldAssignmentReplacesWithConditionalOperator()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string _message;

            private void SetMessageField()
            {
                var now = DateTime.Now;
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                    _message = ""Weekend!"";
                else
                    _message = ""Weekday"";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UseConditionalOperatorAnalyzer.DiagnosticId,
                Message = "If statement can be replaced with a conditional operator.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string _message;

            private void SetMessageField()
            {
                var now = DateTime.Now;
                _message = (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) ? ""Weekend!"" : ""Weekday"";
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void TestConditionalPropertyAssignmentReplacesWithConditionalOperator()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            public string Message { get; set; }

            private void SetMessageProperty()
            {
                var now = DateTime.Now;
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                    Message = ""Weekend!"";
                else
                    Message = ""Weekday"";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UseConditionalOperatorAnalyzer.DiagnosticId,
                Message = "If statement can be replaced with a conditional operator.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            public string Message { get; set; }

            private void SetMessageProperty()
            {
                var now = DateTime.Now;
                Message = (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) ? ""Weekend!"" : ""Weekday"";
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void TestConditionalReturnReplacesWithConditionalOperator()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string GetMessage()
            {
                var now = DateTime.Now;
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                    return ""Weekend!"";
                else
                    return ""Weekday"";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UseConditionalOperatorAnalyzer.DiagnosticId,
                Message = "If statement can be replaced with a conditional operator.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string GetMessage()
            {
                var now = DateTime.Now;
                return (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) ? ""Weekend!"" : ""Weekday"";
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void TestConditionalUnreadVariableAssignmentReplacesWithConditionalOperator()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string GetMessage()
            {
                var message = default(string);
                var now = DateTime.Now;
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                    message = ""Weekend!"";
                else
                    message = ""Weekday"";
                return message;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UseConditionalOperatorAnalyzer.DiagnosticId,
                Message = "If statement can be replaced with a conditional operator.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 12, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string GetMessage()
            {
                var now = DateTime.Now;
                var message = (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) ? ""Weekend!"" : ""Weekday"";
                return message;
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void TestConditionalReadVariableAssignmentReplacesWithConditionalOperator()
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string GetMessage()
            {
                var message = default(string);
                var now = DateTime.Now;
                Console.WriteLine(message);
                if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
                    message = ""Weekend!"";
                else
                    message = ""Weekday"";
                return message;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UseConditionalOperatorAnalyzer.DiagnosticId,
                Message = "If statement can be replaced with a conditional operator.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        public class Class1
        {   
            private string GetMessage()
            {
                var message = default(string);
                var now = DateTime.Now;
                Console.WriteLine(message);
                message = (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) ? ""Weekend!"" : ""Weekday"";
                return message;
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new UseConditionalOperatorAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UseConditionalOperatorAnalyzer();
        }
    }
}
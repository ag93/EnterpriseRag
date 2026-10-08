namespace EnterpriseRag.IntegrationTests.RAG
{
    public class RagEvaluationTestCases
    {
        public static IReadOnlyList<RagEvaluationTestCase> TestCases =>
        [
            new()
            {
                Name = "A350 seating capacity",
                Question =
                    "How many passengers does the CloudWay 24 Airbus A350-900 have?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["320"],
                ExpectedFileNames = ["Airbus A350.pdf"]
            },

            new()
            {
                Name = "A350 business class",
                Question =
                    "How many CloudElite business class seats are on the A350?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["42"],
                ExpectedFileNames = ["Airbus A350.pdf"]
            },

            new()
            {
                Name = "Visa passport validity",
                Question =
                    "How long should my passport be valid before travelling with CloudWay 24?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["6 months"],
                ExpectedFileNames =
                [
                    "Check Your Visa and Passport Requirements.pdf"
                ]
            },

            new()
            {
                Name = "A350 multi-context question",
                Question =
                    "How many passengers does the Airbus A350 have, and how many are in CloudLux, CloudElite, and CloudSaver?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["320", "8","42","238"],
                ExpectedFileNames =
                [
                    "Airbus A350.pdf"
                ]
            },

            new()
            {
                Name = "A350 maximum speed",
                Question =
                    "What is the maximum speed of the CloudWay 24 Airbus A350?",
                AnswerExpected = false,
                ExpectedFileNames = ["Airbus A350.pdf"]
            },

            new()
            {
                Name = "Boeing 737 Max seating",
                Question =
                    "How many passengers does the Boeing 737 Max 10 have?",
                AnswerExpected = false,
                ExpectedFileNames = []
            }
        ];
    }
}

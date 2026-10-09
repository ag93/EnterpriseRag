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
                Name = "A380 maximum seating capacity",
                Question = "How many passengers can the Airbus A380 accommodate in an all-economy configuration?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["853"],
                ExpectedFileNames = ["Airbus A380.pdf"]
            },
            new()
            {
                Name = "Boeing 787 total seating capacity",
                Question = "How many seats are in CloudWay 24's Boeing 787 Dreamliner configuration?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["290"],
                ExpectedFileNames = ["Boeing 787 Dreamliner.pdf"]
            },
            new()
            {
                Name = "A350 and Boeing 787 capacity comparison",
                Question = "Compare the total passenger seating capacities of CloudWay 24's Airbus A350-900 and Boeing 787 Dreamliner.",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["320", "290"],
                ExpectedFileNames =
                [
                    "Airbus A350.pdf",
                    "Boeing 787 Dreamliner.pdf"
                ]
            },
            new()
            {
                Name = "Passport blank page requirement",
                Question = "How many blank passport pages are recommended for visa stamps?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["2 blank pages"],
                ExpectedFileNames =
                [
                    "Check Your Visa and Passport Requirements.pdf"
                ]
            },
            new()
            {
                Name = "Unanswerable Boeing 787 maximum speed",
                Question = "What is the exact maximum operating speed of CloudWay 24's Boeing 787 Dreamliner?",
                AnswerExpected = false,
                ExpectedAnswerFragments = [],
                ExpectedFileNames = []
            },
            new()
            {
                Name = "Boeing 787 CloudElite seat count",
                Question = "How many CloudElite business class seats are on CloudWay 24's Boeing 787 Dreamliner?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["32"],
                ExpectedFileNames = ["Boeing 787 Dreamliner.pdf"]
            },
            new()
            {
                Name = "A350 and Boeing 787 CloudLux entertainment comparison",
                Question = "What are the personal entertainment screen sizes in CloudLux on the Airbus A350 and Boeing 787 Dreamliner?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["21", "22"],
                ExpectedFileNames =
                [
                    "Airbus A350.pdf",
                    "Boeing 787 Dreamliner.pdf"
                ]
            },
            new()
            {
                Name = "Countries with separate visa policies from Schengen",
                Question = "According to the CloudWay 24 travel guide, which countries mentioned have visa policies separate from the Schengen Zone?",
                AnswerExpected = true,
                ExpectedAnswerFragments = ["United Kingdom", "Ireland"],
                ExpectedFileNames =
                [
                    "Check Your Visa and Passport Requirements.pdf"
                ]
            }
        ];
    }
}

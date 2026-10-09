namespace EnterpriseRag.Application.Retrieval;

public static class RetrievalTestCases
{
    public static IReadOnlyList<RetrievalTestCase> All =>
    [
        new()
        {
            Name = "A350 seating capacity",
            Query = "How many passengers does the CloudWay 24 Airbus A350-900 have?",
            ExpectedFileNames = ["Airbus A350.pdf"],
            AnswerExpected = true
        },

        new()
        {
            Name = "A350 first class seats",
            Query = "How many seats are available in CloudLux on the Airbus A350?",
            ExpectedFileNames = ["Airbus A350.pdf"],
            AnswerExpected = true
        },

        new()
        {
            Name = "A350 economy seats",
            Query = "How many CloudSaver economy seats does the Airbus A350 have?",
            ExpectedFileNames = ["Airbus A350.pdf"],
            AnswerExpected = true
        },

        new()
        {
            Name = "Boeing 787 seating capacity",
            Query = "How many passengers can the CloudWay 24 Boeing 787 Dreamliner carry?",
            ExpectedFileNames = ["Boeing 787 Dreamliner.pdf"],
            AnswerExpected = true
        },

        new()
        {
            Name = "A380 seating capacity",
            Query = "What is the typical seating capacity of the Airbus A380?",
            ExpectedFileNames = ["Airbus A380.pdf"],
            AnswerExpected = true
        },

        new()
        {
            Name = "A350 business class",
            Query = "How many CloudElite business class seats are on the A350?",
            ExpectedFileNames = ["Airbus A350.pdf"],
            AnswerExpected = true
        },

        new()
        {
            Name = "Visa passport validity",
            Query = "How long should my passport be valid before travelling with CloudWay 24?",
            ExpectedFileNames =
            [
                "Check Your Visa and Passport Requirements.pdf"
            ],
            AnswerExpected = true
        },

        new()
        {
            Name = "A350 maximum speed",
            Query = "What is the maximum speed of the CloudWay 24 Airbus A350?",
            ExpectedFileNames = ["Airbus A350.pdf"],
            AnswerExpected = false
        },

        new()
        {
            Name = "Boeing 737 Max seating",
            Query = "How many passengers does the Boeing 737 Max 10 have?",
            ExpectedFileNames = [],
            AnswerExpected = false
        },

        new()
        {
            Name = "A350 engine type",
            Query = "What type of engine does the CloudWay 24 Airbus A350 use?",
            ExpectedFileNames = ["Airbus A350.pdf"],
            AnswerExpected = false
        },

        new()
        {
            Name = "A350 multi-chunk",
            Query = "How many passengers does the Airbus A350 have, and how many are in CloudLux, CloudElite, and CloudSaver?",
            ExpectedFileNames = ["Airbus A350.pdf"],
            AnswerExpected = false
        }
    ];
}
/*
*   Create basic terminal introduction
*   Choose difficulty
*   Pick champion from list
*   Setup instructions
*   Connect with Gemini and setup a model
*   Run the actual program, allowing the user to send prompts to the client
*   
*   CURRENT TODO:
*   Fix ordering of tasks.
*   Add logic for question/questionS and so on. 
*   Add comments.
*   Possibly streamline the creation of new difficulties
*/

using Mscc.GenerativeAI;

void Main()
{
    Introduction();

    // Could be moved to seperate function for readability.. I'll ponder on that for a bit and get back to you on that!
    string champion = ChooseRandomChampion();
    Game game = new Game(ChooseDifficulty(), champion, ConnectToAPIAndCreateModel(GetAPIKey(), champion));

    RunGame(game);
}

void Introduction()
{
    Console.WriteLine("Welcome to Guess The Champion");  
}

int ChooseDifficulty()
{
    int d = 0;

    Console.WriteLine("If you would like to play, please proceed by choosing a difficulty level:");
    Console.WriteLine("\t1 - 5 questions");
    Console.WriteLine("\t2 - 10 questions");
    Console.WriteLine("\t3 - 20 questions");
    Console.WriteLine("\t4 - 50 questions");

    // Only declared in local scope
    void DifficultySwitch()
    {
        switch (Console.ReadLine())
        {
            case "1":
                d = 1;
                break;
            case "2":
                d = 2;
                break;
            case "3":
                d = 3;
                break;
            case "4":
                d = 4;
                break;
            default:
                d = 0;
                Console.WriteLine("Unacceptable input, please enter a correct difficulty:");
                break;
        }

        /* 
            Could run this directly in the switch and avoid running the if-statement on every call.
            Decided to put function call in if-statement, to avoid overloading the call-stack if someone enters a wrong input a bunch. 
        */
        if(d == 0)
        {
            DifficultySwitch();
        }
    }

    DifficultySwitch();
    Console.WriteLine("Chosen difficulty registered.");
    return d;
}

string ChooseRandomChampion()
{
    string[] champs = File.ReadLines("ChampionList.txt").ToArray();
    int amount = File.ReadLines("ChampionList.txt").Count();
    int randomNumber = new Random().Next(amount);
    Console.WriteLine("A random Champion has been selected.");
    return champs[randomNumber];
}

// Name really says it all, doesn't it?
GenerativeModel ConnectToAPIAndCreateModel(string apiKey, string champion)
{
    // Context for the AI model (mainly to prevent it from leaking the champion name accidentally.)
    string context = string.Format(
    """
    You will be asked a series of questions, asking you to describe or reveal something about a League of Legends champion.
    You will under no circumstance REVEAL the name of the champion in question. 
    Your answers to the questions will be as descriptive as possible, without revealing the champions name.
    Your answer must not be above the length of 400 characters.

    The champion you will be describing is: {0}
    """
    , champion);
    var systemPrompt = new Content(context);

    // Create a new Google AI model client.
    var genai = new GoogleAI(apiKey);
    var model = genai.GenerativeModel(model: Model.Gemini20Flash, systemInstruction: systemPrompt);
    return model;
}

// Get API key from environment variable
static string GetAPIKey()
{
    try
    {
        var key = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException("API key not found");
        }
        return key;
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        throw;
    }
}

void RunGame(Game game)
{
    Console.WriteLine("All good to go. You have " + game.GetTotalGuesses() + " guesses in total. Please proceed to ask your first question.");

    // Don't worry about this too much. It is purely to keep track of when the user repeatedly chooses to pass on guessing, despite having no questions left.
    int dumbassCounter = 0;

    // Main loop of the game
    while (!game.IsFinished())
    {
        if (game.GetTotalQuestions() > 0)
        {
            Console.WriteLine("You have " + game.GetTotalQuestions() + " questions left. Please enter a question.");
            AwaitQuestionAndReply();
            AskForGuess();
        }
        else
        {
            Console.WriteLine("You have used all your questions.");
            AskForGuess();
            dumbassCounter++;
        }
    }

    /*
    *   Local functions for running the game
    */
    async Task AwaitQuestionAndReply()
    {
        string question = Console.ReadLine();
        if (question != null)
        {
            if (question.Length > 1000)
            {
                Console.WriteLine("Please.. ask a shorter question.");
                AwaitQuestionAndReply();
            }
            else
            {
                string answer = await game.AskPrompt(question);
                Console.WriteLine("Answer: " + answer);
            }
        }
        else
        {
            throw new ArgumentNullException("Invalid User input (recieved null)");
        }
    }

    void AskForGuess()
    {
        Console.WriteLine("Do you wish to take a guess? You have " + game.GetTotalGuesses() + " guesses remaining.");
        Console.WriteLine("Enter 1 to take a guess. Enter 2 to pass.");
        GuessSwitch();

        void GuessSwitch()
        {
            bool correctInput = false;
            switch (Console.ReadLine())
            {
                case "1":
                    correctInput = true;
                    MakeAGuess();
                    break;
                case "2":
                    correctInput = true;
                    if (dumbassCounter > 5)
                    {
                        Console.WriteLine("Stop being a dumbass! You have no questions left, so start using your guesses instead of passing.");
                        dumbassCounter--;
                        MakeAGuess();
                    }
                    break;
                default:
                    Console.WriteLine("Unacceptable input, please enter 1 or 2");
                    correctInput = false;
                    break;
            }

            /* 
                Could run this directly in the switch and avoid running the if-statement on every call.
                Decided to put function call in if-statement, to avoid overloading the call-stack if someone enters a wrong input a bunch. 
            */
            if (!correctInput)
            {
                GuessSwitch();
            }

            void MakeAGuess()
            {
                Console.WriteLine("Please enter the name of the champion you would like to guess (Case-sensitive, only proper capitalization is accepted (Stay mad))");
                string guess = Console.ReadLine();
                if (guess != null)
                {
                    bool correctGuess = game.MakeAGuess(guess);
                    if (correctGuess)
                    {
                        Console.WriteLine("Congratulations. You guessed correctly. The champion was indeed " + guess + ". The game is now over.");
                    }
                    else
                    {
                        Console.WriteLine("Incorrect. You now have " + game.GetTotalGuesses() + " guesses left.");
                        if (game.GetTotalGuesses() <= 0)
                        {
                            Console.WriteLine("You have used all your guesses without getting the right champion. The champion was, in fact, " + game.GetChampion() + ". You lost. The game is now over.");
                        }
                    }
                }
                else
                {
                    throw new ArgumentNullException("Invalid User input (recieved null)");
                }
            }
        }
    }
}

Main();
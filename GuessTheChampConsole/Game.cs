using System.Data;
using System.Threading.Tasks;
using Mscc.GenerativeAI;

class Game
{
    private int difficulty;
    private string champion;
    private GenerativeModel model;
    private Dictionary<int, int> diffToQuestionAmount = new Dictionary<int, int>();
    private int totalGuesses;
    private int totalQuestions;
    private bool gameFinished;

    public Game(int d, string c, GenerativeModel m)
    {
        this.difficulty = d;
        this.champion = c;
        this.model = m;
        this.gameFinished = false;
        this.totalGuesses = 3;

        diffToQuestionAmount = SetupDifficultyDictionary();

        try
        {
            this.totalQuestions = diffToQuestionAmount[difficulty];
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine("Difficulty dictionary does not contain key " + difficulty);
            throw;
        }
    }

    public async Task<string> AskPrompt(string prompt)
    {
        totalQuestions--;
        var answer = await model.GenerateContent(prompt);
        return answer.Text;
    }

    public bool MakeAGuess(string guess)
    {
        totalGuesses--;
        if (guess == champion)
        {
            gameFinished = true;
        }
        else if (totalGuesses <= 0)
        {
            gameFinished = true;
        }
        return guess == champion;
    }

    private Dictionary<int, int> SetupDifficultyDictionary()
    {
        Dictionary<int, int> tempDict = new Dictionary<int, int>
        {
            {1, 5},
            {2, 10},
            {3, 20},
            {4, 50}
        };
        return tempDict;
    }

    public bool IsFinished()
    {
        return gameFinished;
    }

    public int GetTotalGuesses()
    {
        return totalGuesses;
    }

    public int GetTotalQuestions()
    {
        return totalQuestions;
    }

    public string GetChampion()
    {
        if (totalGuesses <= 0)
        {
            return champion;
        }
        else
        {
            return "You're not allowed to access the champion while you still have guesses left.";
        }
    }
}
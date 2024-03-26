using static WebHost.Infrastructure.Services.HashAlgConts;
using System.Globalization;

namespace WebHost.Infrastructure.Services;




public static class HashAlgConts
{
    public const int ALPHABET_SIZE = 33;
    public const int MAX_WORD_LENGTH = 30;
    public const string REDIS_KEY_FOR_DOCUMENTS_SET = "documents";
}


public static class StringSplitExtensions
{
    static TextSplitWithFilerEnumerator SplitWordWithFilterOptimized(this string str)
    {
        // LineSplitEnumerator is a struct so there is no allocation here
        return new TextSplitWithFilerEnumerator(str.AsSpan());
    }

    public static IEnumerable<string> SplitWordWithFilter(this string str)
    {
        List<string> words = new List<string>();

        
        
        foreach (var word in str.SplitWordWithFilterOptimized())
        {
            if (word.Length == 0)
                continue;
            
            words.Add(word.ToString());
        }

        List<string> result = new List<string>();

        for (var i = 1; i < words.Count - 1; i++)
        {
            var threeWord = "";
            threeWord += words[i - 1] + " " + words[i] + " " + words[i + 1];
            result.Add(threeWord);
        }

        return result.Count == 0 ? words : result;
    }

    // Must be a ref struct as it contains a ReadOnlySpan<char>
    public ref struct TextSplitWithFilerEnumerator
    {
        private ReadOnlySpan<char> _allStr;
        public TextSplitWithFilerEnumerator(ReadOnlySpan<char> str)
        {
            _allStr = str;
            Current = default;
        }


        ReadOnlySpan<char> allCaseAllowedSymbols = "йцукенгшщзхъфывапролджэячсмитьбюёЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮЁ".AsSpan();
        
        //ReadOnlySpan<char> allowedSymbolsInLower = "йцукенгшщзхъфывапролджэячсмитьбюё";
        CultureInfo rusCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

        

        

        char[] bufferForToLowerOperation = new char[MAX_WORD_LENGTH];
        
        
        Span<string> notAllowedWords = new string[]
        {
            "он", "на", "который", "ним", "нем", "ней", "до", "после", "через", "из", "за", "от",
            "по", "ради", "для", "по", "со", "без", "которая",
            "близ", "во", "под", "ко", "над", "об",
            "обо", "от", "ото", "перед", "передо", "пред",
            "предо", "подо", "при","про",
            "именно", "также", "то", "благодаря",
            "будто", "вроде", "вопреки", "ввиду",
            "вследствие", "да", "еще", "дабы", "даже", "же",
            "ежели", "если", "бы", "то", "затем",
            "зато", "зачем", "значит", "поэтому", "притом",
            "таки", "следовательно", "ибо", "вдобавок", "или",
            "кабы", "как", "скоро", "словно", "только", "так",
            "когда", "коли", "тому", "кроме", "того", "ли",
            "либо", "лишь", "тем", "нежели", "столько",
            "сколько", "невзирая", "независимо", "несмотря",
            "ни", "но", "однако", "особенно", "оттого", "что",
            "отчего", "подобно", "пока", "покамест", "покоду",
            "после", "поскольку", "потому", "почему", "чем",
            "прежде","всем", "том", "причем", "пускай", "пусть",
            "пор", "более", "тогда", "есть", "тоже", "чуть", "виду",
            "можно", "было", "которые", "могут", "могу", "будет",
            "быть", "был", "будешь",  "будет", "будем",
            "будете", "будут", "был", "была", "были",
            "мог", "мочь", "могу", "можешь", "может",
            "можем", "можете", "могут", "мог", "могла",
            "могло", "могли", "все", "всех",
            "всеми", "выше", "возможность", "возможным",
            "делать", "делают", "делаете",
            "делаем", "делало", "делал",
            "сделать", "сделают", "сделаете",
            "сделаем", "сделало", "сделал",
            "позовлять", "позволяю", "позволяешь",
            "позволяют", "позволяете", "позволял",
            "позволяла", "позволяло", "позволяли",
            "позволил", "позволила", "позволило",
            "позволили", "позволить", "иметь",
            "имею", "имеешь", "имеет", "имеем", "имеете",
            "имеют", "имел", "имела", "имело", "имели", "самой",
            "имеющий", "имевший", "самый", "самая", "самое", "самую",
            "самого", "самых", "самому", "самым", "самым", "самой",
            "самою", "самым", "самыми",
            "позволяющий", "позволявший", "позволяемый",
            "точно", "хотя", "чтоб", "чтобы",
            "мы", "вы", "он", "она", "оно", "они",
            "нас", "вас", "его", "ее", "их",
            "нам", "вам", "ему", "ей", "им",
            "нами", "вами", "ею", "ими",
            "ней", "нем", "них", "меня",
            "мне", "меня", "мной", "мне",
            "себе", "собой", "собою", "себя",
            "различный", "различного",
            "различным", "различная",
            "различной", "различной", "различное",
            "различному", "различным",
            "различном", "такой", "такого",
            "такому", "таким", "таком",
            "такая", "такую", "такою",
            "такое", "такие",
            "таких", "таким", "такие",
            "такими", "разный",
            "разного", "разному",
            "разный",
            "разным", "разном",
            "разная", "разной",
            "разную", "разною",
            "разное", "разных",
            "разному", "разные",
            "разными",
            "кто",  "кого", "кому", "кого",
            "кем", "ком", "чего", "чему",
            "ты", "тебя", "тебе", "тобой",
            "твой", "твое", "твоя", "твои",
            "чей", "чьи", "чья", "чьи",
            "какой", "какое", "какая", "какие",
            "всякий", "всякое", "всякая", "всякие",
            "любой", "любое", "любая", "любые",
            "этот", "это", "эта", "эти",
            "твоего", "твоей", "твоих",
            "чьего", "чьей", "чьих",
            "какого", "каких",
            "всякого", "всякой", "всяких",
            "лобого", "любых",
            "этого", "этой", "этих",
            "твоейму", "твоим",
            "чьему", "чьим",
            "какому", "каким",
            "всякому", "всяким",
            "любому", "любым",
            "этому", "этим",
            "твою", "чьими",
            "какими","всякими", "любыми",
            "этими",
            "твоем", "чьем",
            "каком", "всяком",
            "этом", "сколько",
            "скольких", "скольким",
            "сколькими",
        };
        
        // Needed to be compatible with the foreach operator
        
        public TextSplitWithFilerEnumerator GetEnumerator() => this;

        
        public bool MoveNext()
        {
            if (_allStr.Length == 0) // Reach the end of the string
                return false;

            var currentStr = _allStr;

            var index = currentStr.IndexOfAnyExcept(allCaseAllowedSymbols);

            var separator = index != -1 ? currentStr.Slice(index, 1) : ReadOnlySpan<char>.Empty;

            var word = index != -1 ? currentStr.Slice(0, index) : currentStr;

            var filteredWord = word.Length > 1 && word.Length < MAX_WORD_LENGTH ? word : ReadOnlySpan<char>.Empty;

            var wordIsAbbreviation = false;

            foreach (var upperCaseSym in allCaseAllowedSymbols[ALPHABET_SIZE..])
            {
                if (filteredWord.Length == 0)
                    break;

                int lastSymIndex = filteredWord.Length - 1;
                int middleSymIndex = (filteredWord.Length / 2);
                if (upperCaseSym == filteredWord[lastSymIndex] || upperCaseSym == filteredWord[middleSymIndex])
                {
                    wordIsAbbreviation = true;
                }
            }
            if (!wordIsAbbreviation)
            {
                var countWrited = filteredWord.ToLower(bufferForToLowerOperation, rusCultureInfo);
                var bufferForToLowerOperationSpan = bufferForToLowerOperation.AsSpan();
                filteredWord = bufferForToLowerOperationSpan[0..countWrited];
            }
            bool isNotAllowedWord = false;
            foreach (var fitlerWord in notAllowedWords)
            {
                if (filteredWord.Length == 0)
                    break;
                var spanFilterWord = fitlerWord.AsSpan();
                if (spanFilterWord.Length == filteredWord.Length)
                {
                    bool wordAreEqual = true;
                    for (int i = 0; i < filteredWord.Length; i++)
                    {
                        if (spanFilterWord[i] != filteredWord[i])
                        {
                            wordAreEqual = false;
                            break;
                        }

                    }
                    if (wordAreEqual)
                    {
                        isNotAllowedWord = true;
                        break;
                    }

                }
            }

            Current = isNotAllowedWord ? ReadOnlySpan<char>.Empty : filteredWord;

            //_wordHasher.GetHashedWord(word: filteredWord, hashedWord: ref bufferForHash, threeWords: ref bufferForThreeWords);
            //Current = new ThreeHashedWordsEntry(bufferForHash, bufferForThreeWords, separator);

            _allStr = index != -1 ? currentStr.Slice(index + 1) : ReadOnlySpan<char>.Empty;

            return true;
        }
        
        
        public ReadOnlySpan<char> Current { get; private set; }
    }

}




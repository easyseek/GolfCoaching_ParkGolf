using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class VirtualKeyboard : MonoBehaviour
{
    public static VirtualKeyboard Instance { get; private set; }

    [SerializeField] private TMP_InputField[] targets;
    [SerializeField] private bool startKorean = true;
    [SerializeField] private bool autoFindButtons = true;
    [SerializeField] private bool logDebug = false;

    private bool _isKorean;
    private bool _shift;
    private bool _caps;

    private TMP_InputField currentTarget;

    private HangulState _state = new HangulState();

    private static readonly Dictionary<string, (string normal, string shift)> _enMap = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
    {
        // 숫자열
        ["1"] = ("1", "!"),
        ["2"] = ("2", "@"),
        ["3"] = ("3", "#"),
        ["4"] = ("4", "$"),
        ["5"] = ("5", "%"),
        ["6"] = ("6", "^"),
        ["7"] = ("7", "&"),
        ["8"] = ("8", "*"),
        ["9"] = ("9", "("),
        ["0"] = ("0", ")"),
        ["-"] = ("-", "_"),
        ["="] = ("=", "+"),

        // 영문 기본
        ["q"] = ("q", "Q"),
        ["w"] = ("w", "W"),
        ["e"] = ("e", "E"),
        ["r"] = ("r", "R"),
        ["t"] = ("t", "T"),
        ["y"] = ("y", "Y"),
        ["u"] = ("u", "U"),
        ["i"] = ("i", "I"),
        ["o"] = ("o", "O"),
        ["p"] = ("p", "P"),
        ["a"] = ("a", "A"),
        ["s"] = ("s", "S"),
        ["d"] = ("d", "D"),
        ["f"] = ("f", "F"),
        ["g"] = ("g", "G"),
        ["h"] = ("h", "H"),
        ["j"] = ("j", "J"),
        ["k"] = ("k", "K"),
        ["l"] = ("l", "L"),
        ["z"] = ("z", "Z"),
        ["x"] = ("x", "X"),
        ["c"] = ("c", "C"),
        ["v"] = ("v", "V"),
        ["b"] = ("b", "B"),
        ["n"] = ("n", "N"),
        ["m"] = ("m", "M"),

        ["["] = ("[", "{"),
        ["]"] = ("]", "}"),
        [";"] = (";", ":"),
        ["'"] = ("'", "\""),
        [","] = (",", "<"),
        ["."] = (".", ">"),
        ["/"] = ("/", "?"),
        ["`"] = ("`", "~"),
        ["\\"] = ("\\", "|"),
        ["Space"] = (" ", " "),
        ["Tab"] = ("\t", "\t")
    };

    private static readonly Dictionary<string, Jamo> _krMap = new Dictionary<string, Jamo>(StringComparer.OrdinalIgnoreCase)
    {
        ["r"] = J(Con.ㄱ, Con.ㄲ),
        ["s"] = J(Con.ㄴ),
        ["e"] = J(Con.ㄷ, Con.ㄸ),
        ["f"] = J(Con.ㄹ),
        ["a"] = J(Con.ㅁ),
        ["q"] = J(Con.ㅂ, Con.ㅃ),
        ["t"] = J(Con.ㅅ, Con.ㅆ),
        ["d"] = J(Con.ㅇ),
        ["w"] = J(Con.ㅈ, Con.ㅉ),
        ["c"] = J(Con.ㅊ),
        ["z"] = J(Con.ㅋ),
        ["x"] = J(Con.ㅌ),
        ["v"] = J(Con.ㅍ),
        ["g"] = J(Con.ㅎ),

        ["k"] = J(Vow.ㅏ),
        ["o"] = J(Vow.ㅐ, Vow.ㅒ),
        ["i"] = J(Vow.ㅑ),
        ["j"] = J(Vow.ㅓ),
        ["p"] = J(Vow.ㅔ, Vow.ㅖ),
        ["u"] = J(Vow.ㅕ),
        ["h"] = J(Vow.ㅗ),
        ["y"] = J(Vow.ㅛ),
        ["n"] = J(Vow.ㅜ),
        ["b"] = J(Vow.ㅠ),
        ["m"] = J(Vow.ㅡ),
        ["l"] = J(Vow.ㅣ),
    };

    private static Jamo J(Con baseCon, Con? shiftDouble = null)
    {
        return new Jamo { con = baseCon, conShift = shiftDouble };
    }

    private static Jamo J(Vow baseVow, Vow? shiftVow = null)
    {
        return new Jamo { vow = baseVow, vowShift = shiftVow };
    }

    private void Awake()
    {
        Instance = this;

        _isKorean = startKorean;

        if (targets != null && targets.Length > 0)
        {
            currentTarget = targets[0];
        }

        if (autoFindButtons)
        {
            WireButtons();
        }

        RefreshShiftVisual(false);
        RefreshLangVisual();

        gameObject.SetActive(false);
    }

    private void WireButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            string key = btn.gameObject.name.Trim();

            btn.onClick.AddListener(() =>
            {
                OnKey(key);
            });
        }
    }

    public void ShowFor(TMP_InputField field)
    {
        currentTarget = field;

        if (currentTarget != null)
        {
            currentTarget.ActivateInputField();
            currentTarget.Select();
        }

        gameObject.SetActive(true);
    }

    public void ShowForIndex(int index)
    {
        if (targets == null)
        {
            return;
        }

        if (index < 0 || index >= targets.Length)
        {
            return;
        }

        ShowFor(targets[index]);
    }

    public TMP_InputField CurrentTarget
    {
        get { return currentTarget; }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnKey(string keyName)
    {
        if (string.IsNullOrEmpty(keyName))
        {
            return;
        }
        keyName = keyName.Trim();

        if (logDebug)
        {
            Debug.Log("[VK] Key: " + keyName);
        }

        switch (keyName)
        {
            case "Backspace":
                {
                    HandleBackspace();
                    return;
                }
            case "Enter":
            case "Enter/Return":
                {
                    // InsertText("\n");
                    // CommitIfComposing();
                    gameObject.SetActive(false);
                    return;
                }
            case "Space":
                {
                    InsertText(" ");
                    CommitIfComposing();
                    return;
                }
            case "Tab":
                {
                    InsertText("\t");
                    CommitIfComposing();
                    return;
                }
            case "Shift":
                {
                    _shift = !_shift;
                    RefreshShiftVisual(_shift);
                    return;
                }
            case "ShiftUp":
                {
                    _shift = false;
                    RefreshShiftVisual(false);
                    return;
                }
            case "Caps Lock":
            case "CapsLock":
                {
                    _caps = !_caps;
                    return;
                }
            case "한영":
            case "Lang":
                {
                    _isKorean = !_isKorean;
                    CommitIfComposing();
                    RefreshLangVisual();
                    return;
                }
            case "Close":
            case "XBtn":
            case "BG":
                {
                    gameObject.SetActive(false);
                    return;
                }
        }

        if (_isKorean)
        {
            HandleKoreanKey(keyName);
        }
        else
        {
            HandleEnglishKey(keyName);
        }
    }

    // ---------- English ----------
    private void HandleEnglishKey(string key)
    {
        (string normal, string shift) mapping;

        if (!_enMap.TryGetValue(key, out mapping))
        {
            string lk = key.ToLowerInvariant();
            if (!_enMap.TryGetValue(lk, out mapping))
            {
                return;
            }
        }

        bool upper = _shift ^ _caps;
        string s = upper ? mapping.shift : mapping.normal;

        InsertText(s);

        if (_shift)
        {
            _shift = false;
            RefreshShiftVisual(false);
        }
    }

    // ---------- Korean ----------
    private void HandleKoreanKey(string key)
    {
        if (_enMap.ContainsKey(key) && !_krMap.ContainsKey(key))
        {
            HandleEnglishWhenKorean(key);
            return;
        }

        Jamo jamo;

        if (!_krMap.TryGetValue(key, out jamo))
        {
            string lk = key.ToLowerInvariant();

            if (!_krMap.TryGetValue(lk, out jamo))
            {
                HandleEnglishWhenKorean(key);
                return;
            }
        }

        Vow? vow = jamo.vow;
        Con? con = jamo.con;

        bool wantShift = _shift ^ _caps;

        if (wantShift)
        {
            if (jamo.vowShift.HasValue)
            {
                vow = jamo.vowShift;
            }
            if (jamo.conShift.HasValue)
            {
                con = jamo.conShift;
            }
        }

        bool consumed = false;

        if (vow.HasValue)
        {
            consumed = _state.InputVowel(vow.Value);
        }
        else if (con.HasValue)
        {
            consumed = _state.InputConsonant(con.Value, allowDoubleByTyping: false); // 된소리 타이핑 금지
        }

        ApplyCompositionToField();

        if (_shift)
        {
            _shift = false;
            RefreshShiftVisual(false);
        }
    }

    private void HandleEnglishWhenKorean(string key)
    {
        HandleEnglishKey(key);
        CommitIfComposing();
    }

    private void HandleBackspace()
    {
        if (_isKorean && _state.Backspace())
        {
            ApplyCompositionToField();
            return;
        }

        if (currentTarget == null)
        {
            return;
        }

        int start = Mathf.Min(currentTarget.selectionAnchorPosition, currentTarget.selectionFocusPosition);
        int end = Mathf.Max(currentTarget.selectionAnchorPosition, currentTarget.selectionFocusPosition);

        if (start != end)
        {
            currentTarget.text = currentTarget.text.Remove(start, end - start);
            currentTarget.caretPosition = start;
        }
        else if (start > 0)
        {
            currentTarget.text = currentTarget.text.Remove(start - 1, 1);
            currentTarget.caretPosition = start - 1;
        }
    }

    private void InsertText(string s)
    {
        if (currentTarget == null)
        {
            return;
        }

        int start = Mathf.Min(currentTarget.selectionAnchorPosition, currentTarget.selectionFocusPosition);
        int end = Mathf.Max(currentTarget.selectionAnchorPosition, currentTarget.selectionFocusPosition);

        if (start != end)
        {
            currentTarget.text = currentTarget.text.Remove(start, end - start);
        }

        currentTarget.text = currentTarget.text.Insert(start, s);
        currentTarget.caretPosition = start + s.Length;
        currentTarget.selectionAnchorPosition = currentTarget.caretPosition;
        currentTarget.selectionFocusPosition = currentTarget.caretPosition;
    }

    private void ApplyCompositionToField()
    {
        if (currentTarget == null)
        {
            return;
        }

        if (_state.HasRewritePending)
        {
            int caretReplace = Mathf.Max(0, currentTarget.caretPosition - 1);

            if (currentTarget.text.Length > 0 && caretReplace < currentTarget.text.Length)
            {
                string rep = _state.ConsumeRewriteCharAsString();
                currentTarget.text = currentTarget.text.Remove(caretReplace, 1).Insert(caretReplace, rep);
                currentTarget.caretPosition = caretReplace + 1;
                currentTarget.selectionAnchorPosition = currentTarget.caretPosition;
                currentTarget.selectionFocusPosition = currentTarget.caretPosition;

                _state.hasAnchor = false;
            }
            else
            {
                _state.ClearRewrite();
            }
        }

        string composed = _state.GetComposedString();

        if (composed.Length == 0)
        {
            return;
        }

        if (!_state.hasAnchor)
        {
            InsertText(composed);
            _state.hasAnchor = true;
            return;
        }

        int caret = currentTarget.caretPosition;
        int pos = Mathf.Max(0, caret - 1);

        if (currentTarget.text.Length > 0 && pos < currentTarget.text.Length)
        {
            currentTarget.text = currentTarget.text.Remove(pos, 1).Insert(pos, composed);
            currentTarget.caretPosition = pos + 1;
            currentTarget.selectionAnchorPosition = currentTarget.caretPosition;
            currentTarget.selectionFocusPosition = currentTarget.caretPosition;
        }
        else
        {
            InsertText(composed);
        }
    }

    private void CommitIfComposing()
    {
        if (_state.IsEmpty)
        {
            return;
        }

        _state.Reset();
    }

    private void RefreshShiftVisual(bool onFlag)
    {
        if (logDebug)
        {
            Debug.Log("[VK] Shift=" + (onFlag ? "ON" : "OFF"));
        }
    }

    private void RefreshLangVisual()
    {
        if (logDebug)
        {
            Debug.Log("[VK] Lang=" + (_isKorean ? "KOR" : "ENG"));
        }
    }

    private struct Jamo
    {
        public Con? con;
        public Con? conShift;
        public Vow? vow;
        public Vow? vowShift;
    }

    private enum Con
    {
        ㄱ, ㄲ, ㄴ, ㄷ, ㄸ, ㄹ, ㅁ, ㅂ, ㅃ, ㅅ, ㅆ, ㅇ, ㅈ, ㅉ, ㅊ, ㅋ, ㅌ, ㅍ, ㅎ
    }

    private enum Vow
    {
        ㅏ, ㅐ, ㅑ, ㅒ, ㅓ, ㅔ, ㅕ, ㅖ, ㅗ, ㅘ, ㅙ, ㅚ, ㅛ, ㅜ, ㅝ, ㅞ, ㅟ, ㅠ, ㅡ, ㅢ, ㅣ
    }

    private class HangulState
    {
        private static readonly string CHO = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        private static readonly string JUNG = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        private static readonly string JONG = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

        private static readonly Dictionary<char, Dictionary<char, char>> COMBO_V = new Dictionary<char, Dictionary<char, char>>
        {
            ['ㅗ'] = new Dictionary<char, char> { { 'ㅏ', 'ㅘ' }, { 'ㅐ', 'ㅙ' }, { 'ㅣ', 'ㅚ' } },
            ['ㅜ'] = new Dictionary<char, char> { { 'ㅓ', 'ㅝ' }, { 'ㅔ', 'ㅞ' }, { 'ㅣ', 'ㅟ' } },
            ['ㅡ'] = new Dictionary<char, char> { { 'ㅣ', 'ㅢ' } },
        };

        private static readonly Dictionary<char, Dictionary<char, char>> COMBO_J = new Dictionary<char, Dictionary<char, char>>
        {
            ['ㄱ'] = new Dictionary<char, char> { { 'ㅅ', 'ㄳ' } },
            ['ㄴ'] = new Dictionary<char, char> { { 'ㅈ', 'ㄵ' }, { 'ㅎ', 'ㄶ' } },
            ['ㄹ'] = new Dictionary<char, char> { { 'ㄱ', 'ㄺ' }, { 'ㅁ', 'ㄻ' }, { 'ㅂ', 'ㄼ' }, { 'ㅅ', 'ㄽ' }, { 'ㅌ', 'ㄾ' }, { 'ㅍ', 'ㄿ' }, { 'ㅎ', 'ㅀ' } },
            ['ㅂ'] = new Dictionary<char, char> { { 'ㅅ', 'ㅄ' } },
        };

        public int cho = -1;
        public int jung = -1;
        public int jong = -1;

        public bool hasAnchor = false;

        private char rewritePrev = '\0';

        public bool HasRewritePending
        {
            get { return rewritePrev != '\0'; }
        }

        public string ConsumeRewriteCharAsString()
        {
            string s = new string(new char[] { rewritePrev });

            rewritePrev = '\0';

            return s;
        }

        public void ClearRewrite()
        {
            rewritePrev = '\0';
        }

        public bool IsEmpty
        {
            get { return cho < 0 && jung < 0 && jong < 0; }
        }

        public void Reset()
        {
            cho = -1;
            jung = -1;
            jong = -1;
            hasAnchor = false;
            rewritePrev = '\0';
        }

        public bool InputConsonant(Con conIn, bool allowDoubleByTyping)
        {
            char c = ToChar(conIn);

            if (cho < 0 && jung < 0 && jong < 0)
            {
                cho = CHO.IndexOf(c);
                return true;
            }

            if (cho >= 0 && jung < 0)
            {
                if (!allowDoubleByTyping)
                {
                    hasAnchor = false;
                    cho = CHO.IndexOf(c);
                    return true;
                }

                string doubled = ChoDouble(CHO[cho], c);

                if (doubled != null)
                {
                    cho = CHO.IndexOf(doubled[0]);
                    return true;
                }

                hasAnchor = false;
                cho = CHO.IndexOf(c);
                return true;
            }

            if (cho >= 0 && jung >= 0 && jong < 0)
            {
                int jidx = JONG.IndexOf(c);

                if (jidx >= 0)
                {
                    jong = jidx;
                    return true;
                }
            }

            if (cho >= 0 && jung >= 0 && jong >= 0)
            {
                if (jong > 0)
                {
                    Dictionary<char, char> dict;
                    char baseJ = JONG[jong];

                    if (COMBO_J.TryGetValue(baseJ, out dict) && dict.TryGetValue(c, out char combo))
                    {
                        jong = JONG.IndexOf(combo);
                        return true;
                    }
                }

                hasAnchor = false;
                cho = CHO.IndexOf(c);
                jung = -1;
                jong = -1;
                return true;
            }

            return false;
        }

        public bool InputVowel(Vow vowIn)
        {
            char v = ToChar(vowIn);

            if (cho < 0 && jung < 0 && jong < 0)
            {
                jung = JUNG.IndexOf(v);
                return true;
            }

            if (cho >= 0 && jung < 0)
            {
                jung = JUNG.IndexOf(v);
                return true;
            }

            if (jung >= 0 && jong < 0)
            {
                char baseV = JUNG[jung];
                Dictionary<char, char> dict;

                if (COMBO_V.TryGetValue(baseV, out dict) && dict.TryGetValue(v, out char combo))
                {
                    jung = JUNG.IndexOf(combo);
                    return true;
                }

                hasAnchor = false;
                cho = -1;
                jung = JUNG.IndexOf(v);
                return true;
            }

            if (jong > 0)
            {
                char jongChar = JONG[jong];

                (char remain, char carry) general = SplitJongGeneral(jongChar);
                int prevCho = cho;
                int prevJung = jung;
                int newPrevJong = (general.remain == '\0') ? 0 : JONG.IndexOf(general.remain);

                int backupCho = cho;
                int backupJung = jung;
                int backupJong = jong;

                cho = prevCho;
                jung = prevJung;
                jong = newPrevJong;
                string prevChar = GetComposedString();
                if (!string.IsNullOrEmpty(prevChar))
                {
                    rewritePrev = prevChar[0];
                }

                cho = (general.carry != '\0') ? CHO.IndexOf(general.carry) : -1;
                jung = JUNG.IndexOf(v);
                jong = -1;

                hasAnchor = false;

                return true;
            }

            return false;
        }

        public bool Backspace()
        {
            if (jong > 0)
            {
                char baseJ = JONG[jong];
                (char first, char second) pair = SplitJongSpecific(baseJ);
                if (pair.second != '\0')
                {
                    jong = JONG.IndexOf(pair.first);
                }
                else
                {
                    jong = -1;
                }
                return true;
            }

            if (jung >= 0)
            {
                char baseV = JUNG[jung];
                char decomp = DecomposeVowel(baseV);
                if (decomp != '\0')
                {
                    jung = JUNG.IndexOf(decomp);
                }
                else
                {
                    jung = -1;
                }
                return true;
            }

            if (cho >= 0)
            {
                cho = -1;
                hasAnchor = false;
                return false;
            }

            return false;
        }

        public string GetComposedString()
        {
            if (cho < 0 && jung < 0 && jong < 0)
            {
                return "";
            }

            if (cho >= 0 && jung >= 0)
            {
                int jongIdx = Mathf.Max(jong, 0);
                int code = 0xAC00 + ((cho * 21) + jung) * 28 + jongIdx;
                return char.ConvertFromUtf32(code);
            }

            if (cho >= 0 && jung < 0)
            {
                return CHO[cho].ToString();
            }

            if (jung >= 0 && cho < 0)
            {
                return JUNG[jung].ToString();
            }

            return "";
        }

        private static string ChoDouble(char a, char b)
        {
            if (a == 'ㄱ' && b == 'ㄱ') return "ㄲ";
            if (a == 'ㄷ' && b == 'ㄷ') return "ㄸ";
            if (a == 'ㅂ' && b == 'ㅂ') return "ㅃ";
            if (a == 'ㅅ' && b == 'ㅅ') return "ㅆ";
            if (a == 'ㅈ' && b == 'ㅈ') return "ㅉ";
            return null;
        }

        private static (char first, char second) SplitJongSpecific(char jongChar)
        {
            switch (jongChar)
            {
                case 'ㄳ': return ('ㄱ', 'ㅅ');
                case 'ㄵ': return ('ㄴ', 'ㅈ');
                case 'ㄶ': return ('ㄴ', 'ㅎ');
                case 'ㄺ': return ('ㄹ', 'ㄱ');
                case 'ㄻ': return ('ㄹ', 'ㅁ');
                case 'ㄼ': return ('ㄹ', 'ㅂ');
                case 'ㄽ': return ('ㄹ', 'ㅅ');
                case 'ㄾ': return ('ㄹ', 'ㅌ');
                case 'ㄿ': return ('ㄹ', 'ㅍ');
                case 'ㅀ': return ('ㄹ', 'ㅎ');
                case 'ㅄ': return ('ㅂ', 'ㅅ');
                default: return ('\0', '\0');
            }
        }

        private static (char remain, char carry) SplitJongGeneral(char jongChar)
        {
            (char first, char second) pair = SplitJongSpecific(jongChar);
            if (pair.first != '\0' || pair.second != '\0')
            {
                return (pair.first, pair.second);
            }

            if (jongChar != ' ')
            {
                return ('\0', jongChar);
            }
            return ('\0', '\0');
        }

        private static char DecomposeVowel(char jungChar)
        {
            switch (jungChar)
            {
                case 'ㅘ': return 'ㅗ';
                case 'ㅙ': return 'ㅗ';
                case 'ㅚ': return 'ㅗ';
                case 'ㅝ': return 'ㅜ';
                case 'ㅞ': return 'ㅜ';
                case 'ㅟ': return 'ㅜ';
                case 'ㅢ': return 'ㅡ';
                default: return '\0';
            }
        }

        private static char ToChar(Con c)
        {
            return "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ"[(int)c];
        }

        private static char ToChar(Vow v)
        {
            return "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ"[(int)v];
        }
    }
}

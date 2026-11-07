using UnityEngine;

namespace AfterSignal
{
    /// <summary>Placeholder kept in sync with the gameplay branch; replaced by the full implementation on merge.</summary>
    public static class DifficultySettings
    {
        public static readonly string[] Names = { "쉬움", "보통", "어려움" };
        static int difficulty = -1;
        public static int Difficulty
        {
            get { if (difficulty < 0) difficulty = Mathf.Clamp(PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Difficulty", 1), 0, 2); return difficulty; }
            set { difficulty = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Difficulty", difficulty); PlayerPrefs.Save(); }
        }
    }
}

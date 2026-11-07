using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CityLife
    {
        /// <summary>Recent story and dialogue lines, newest first, five per page.</summary>
        public void DialogueHistory(int page = 0)
        {
            var log = GameDirector.DialogueLog;
            int pages = Mathf.Max(1, (log.Count + 4) / 5);
            page = Mathf.Clamp(page, 0, pages - 1);
            int current = page;
            var text = new System.Text.StringBuilder();
            if (log.Count == 0) text.Append("아직 기록된 대화가 없습니다.");
            for (int i = log.Count - 1 - page * 5, shown = 0; i >= 0 && shown < 5; i--, shown++)
                text.Append("◆ ").Append(log[i].title).Append('\n').Append(log[i].text.Trim()).Append("\n\n");
            Panel("journal", "최근 대화 기록 · " + (page + 1) + " / " + pages, text.ToString().TrimEnd());
            if (current > 0) Option("최근 쪽으로", () => DialogueHistory(current - 1));
            if (current + 1 < pages) Option("이전 대화", () => DialogueHistory(current + 1));
            Option("일지로", StoryJournal);
            Option("닫기", Dismiss);
        }
    }
}

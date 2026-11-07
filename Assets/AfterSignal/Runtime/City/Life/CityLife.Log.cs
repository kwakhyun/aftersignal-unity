using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CityLife
    {
        const int HistoryPerPage = 3;
        /// <summary>Recent story and dialogue lines, newest first. Three per page fit the 320 px journal body; a longer page scrolls with the wheel.</summary>
        public void DialogueHistory(int page = 0)
        {
            var log = GameDirector.DialogueLog;
            int pages = Mathf.Max(1, (log.Count + HistoryPerPage - 1) / HistoryPerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            int current = page;
            var text = new System.Text.StringBuilder();
            if (log.Count == 0) text.Append("아직 기록된 대화가 없습니다.");
            for (int i = log.Count - 1 - page * HistoryPerPage, shown = 0; i >= 0 && shown < HistoryPerPage; i--, shown++)
                text.Append("◆ ").Append(log[i].title).Append('\n').Append(log[i].text.Trim()).Append("\n\n");
            Panel("journal", "최근 대화 기록 · " + (page + 1) + " / " + pages, text.ToString().TrimEnd());
            if (current > 0) Option("최근 쪽으로", () => DialogueHistory(current - 1));
            if (current + 1 < pages) Option("이전 대화", () => DialogueHistory(current + 1));
            Option("일지로", StoryJournal);
            Option("닫기", Dismiss);
        }
    }
}

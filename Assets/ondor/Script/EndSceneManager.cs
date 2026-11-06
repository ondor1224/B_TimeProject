using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndSceneManager : MonoBehaviour
{
    public Text scoreText, rankListText;
    public InputField nameInput;
    public AudioClip sceneBgm;
    bool submitted;

    void Start()
    {
        Time.timeScale = 1;
        if (sceneBgm) SoundManager.PlayBgm(sceneBgm);
        if (scoreText) scoreText.text = $"STAGE {RunData.Stage} | SCORE: {RunData.Score} | GOLD: {RunData.Money}";
        RefreshRankBoard();
    }

    public void SubmitRank()
    {
        if (submitted) return;
        submitted = true;
        string id = (nameInput && !string.IsNullOrWhiteSpace(nameInput.text)) ? nameInput.text : "PLAYER";
        if (Ranking.Qualifies(RunData.Score)) Ranking.Add(id, RunData.Score, RunData.Money);
        RefreshRankBoard();
    }

    void RefreshRankBoard()
    {
        if (!rankListText) return;
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var t = typeof(Ranking);
        var loadMethod = t.GetMethod("Load", flags);
        object raw = loadMethod != null && loadMethod.GetParameters().Length == 0 ? loadMethod.Invoke(null, null) : null;

        var list = raw as IEnumerable;
        if (list == null)
        {
            foreach (var f in t.GetFields(flags))
                if (typeof(IEnumerable).IsAssignableFrom(f.FieldType) && f.FieldType != typeof(string))
                { list = f.GetValue(null) as IEnumerable; if (list != null) break; }
        }
        if (list == null)
        {
            foreach (var p in t.GetProperties(flags))
                if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
                { list = p.GetValue(null) as IEnumerable; if (list != null) break; }
        }

        string s = "=== RANKING ===\n";
        if (list != null)
        {
            int rank = 1;
            foreach (var item in list)
            {
                if (item == null) continue;
                var it = item.GetType();
                string id = "PLAYER";
                int score = 0, money = 0, intIdx = 0;
                foreach (var f in it.GetFields(flags))
                {
                    var v = f.GetValue(item);
                    if (f.FieldType == typeof(string) && v != null) id = v.ToString();
                    else if (f.FieldType == typeof(int) && v is int iv)
                    {
                        if (intIdx == 0) score = iv;
                        else if (intIdx == 1) money = iv;
                        intIdx++;
                    }
                }
                s += $"{rank++}. {id} - {score} pts ({money}G)\n";
            }
        }
        rankListText.text = s;
    }

    string GetTargetGameScene()
    {
        return (string.IsNullOrEmpty(RunData.GameScene) || RunData.GameScene == SceneManager.GetActiveScene().name)
            ? "Game"
            : RunData.GameScene;
    }

    public void RetryKeepData()
    {
        SceneManager.LoadScene(GetTargetGameScene());
    }

    public void RestartFromBeginning()
    {
        RunData.Reset();
        SceneManager.LoadScene(GetTargetGameScene());
    }
}

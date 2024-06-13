using Quantum;
using UnityEngine;

public class Utils : MonoBehaviour {

    private static readonly Color[] PlayerColors = {
        Color.yellow,
        Color.red,
        Color.cyan,
        Color.magenta,
        Color.green,
        new(1, 0, 1)
    };

    public static Color GetPlayerColor(Frame frame, EntityRef entity) {
        Color color = Color.gray;
        if (frame.TryGet(entity, out PlayerLink pl)) {
            color = PlayerColors[(pl.Player._index - 1) % PlayerColors.Length];
        }

        return color;
    }

    public static string RankingToString(int ranking) {

        ranking = Mathf.Abs(ranking);

        int lastNumber = ranking % 10;
        char character;

        // what.
        if (ranking < 10 || ranking >= 20) {
            character = lastNumber switch {
                1 => 'A',
                2 => 'B',
                3 => 'C',
                _ => 'D',
            };
        } else {
            character = 'D';
        }

        return ranking.ToString() + character;
    }

}


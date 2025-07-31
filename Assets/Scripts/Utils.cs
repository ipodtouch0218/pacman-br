using Quantum;
using System.Collections.Generic;
using UnityEngine;

public unsafe static class Utils {

    private static readonly Color[] PlayerColors = {
        Color.yellow,
        Color.red,
        Color.cyan,
        Color.magenta,
        Color.green,
        new(0.5f, 0, 1)
    };

    public static Color GetPlayerColor(Frame f, EntityRef entity) {
        Color color = Color.gray;
        if (f.Unsafe.TryGetPointer(entity, out PlayerLink* pl)) {
            color = PlayerColors[(pl->Player) % PlayerColors.Length];
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

    public static void SetIfNull<T>(this Component component, ref T var, GetComponentType children = GetComponentType.Self) where T : Component {
        if (component && !var) {
            var = children switch {
                GetComponentType.Children => component.GetComponentInChildren<T>(),
                GetComponentType.Parent => component.GetComponentInParent<T>(),
                _ => component.GetComponent<T>(),
            };
        }
    }

    public static void SetIfNull<T>(this Component component, ref T[] var, GetComponentType children = GetComponentType.Self) where T : Component {
        if (component && (var == null || var.Length <= 0)) {
            var = children switch {
                GetComponentType.Children => component.GetComponentsInChildren<T>(),
                GetComponentType.Parent => component.GetComponentsInParent<T>(),
                _ => component.GetComponents<T>(),
            };
        }
    }

    public static void SetIfNull<T>(this Component component, ref List<T> var, GetComponentType children = GetComponentType.Self) where T : Component {
        if (component && (var == null || var.Count <= 0)) {
            switch (children) {
            case GetComponentType.Children: component.GetComponentsInChildren(var); break;
            case GetComponentType.Parent: component.GetComponentsInParent(false, var); break; // Why doesn't this have a default == false version? wtf?
            default: component.GetComponents(var); break;
            };
        }
    }

    public enum GetComponentType {
        Self,
        Parent,
        Children
    }
}


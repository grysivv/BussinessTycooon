using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralny system zdarzeń. Umożliwia komunikację między systemami
/// bez bezpośrednich referencji. Wszystkie subskrypcje muszą być
/// wyrejestrowane w OnDisable aby uniknąć wycieków pamięci.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers
        = new Dictionary<Type, List<Delegate>>();

    /// <summary>
    /// Subskrybuj zdarzenie typu T.
    /// Zawsze wywołuj Unsubscribe w OnDisable.
    /// </summary>
    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);

        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();

        _handlers[type].Add(handler);
    }

    /// <summary>
    /// Wyrejestruj subskrypcję. Wymagane w OnDisable każdego subskrybenta.
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);

        if (!_handlers.ContainsKey(type))
            return;

        _handlers[type].Remove(handler);
    }

    /// <summary>
    /// Opublikuj zdarzenie. Wszyscy subskrybenci zostaną powiadomieni.
    /// </summary>
    public static void Publish<T>(T eventData)
    {
        var type = typeof(T);

        if (!_handlers.ContainsKey(type))
            return;

        // Kopia listy — zabezpieczenie przed modyfikacją podczas iteracji
        var handlers = new List<Delegate>(_handlers[type]);

        foreach (var handler in handlers)
        {
            try
            {
                ((Action<T>)handler)?.Invoke(eventData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventBus] Błąd w handlerze {handler.Method.Name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Czyści wszystkie subskrypcje. Wywołaj przy zmianie sceny.
    /// </summary>
    public static void Clear()
    {
        _handlers.Clear();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Diagnostyka — ile handlerów jest zarejestrowanych.
    /// Tylko w edytorze, do debugowania wycieków.
    /// </summary>
    public static void LogStats()
    {
        foreach (var kvp in _handlers)
            Debug.Log($"[EventBus] {kvp.Key.Name}: {kvp.Value.Count} handler(s)");
    }
#endif
}
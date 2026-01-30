using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized service locator for component discovery and caching.
///
/// Why: FindAnyObjectByType is called 50+ times across the codebase, creating
/// performance overhead and code duplication.
///
/// Rationale: Single registry with caching reduces redundant scene searches and
/// provides a clear dependency injection point for testing.
///
/// Usage:
///   ServiceRegistry.Register(this);  // In Awake()
///   var grid = ServiceRegistry.Get<GridManager>();
/// </summary>
public class ServiceRegistry : MonoBehaviour
{
    private static ServiceRegistry _instance;
    private readonly Dictionary<Type, Component> _services = new Dictionary<Type, Component>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            DebugLog.Info("[ServiceRegistry] Initialized");
        }
        else if (_instance != this)
        {
            DebugLog.LogWarning("[ServiceRegistry] Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Gets a service of the specified type, caching it for future requests.
    /// First call does a FindAnyObjectByType, subsequent calls use cache.
    /// </summary>
    /// <typeparam name="T">Component type to find</typeparam>
    /// <returns>Service instance or null if not found</returns>
    public static T Get<T>() where T : Component
    {
        if (_instance == null)
        {
            DebugLog.LogWarning($"[ServiceRegistry] No instance exists, creating one");
            GameObject registryGO = new GameObject("ServiceRegistry");
            _instance = registryGO.AddComponent<ServiceRegistry>();
        }

        Type serviceType = typeof(T);

        // Check cache first
        if (_instance._services.TryGetValue(serviceType, out Component cachedService))
        {
            if (cachedService != null)
            {
                return cachedService as T;
            }
            else
            {
                // Cached service was destroyed, remove from cache
                _instance._services.Remove(serviceType);
            }
        }

        // Not in cache, find and cache it
        T found = UnityEngine.Object.FindAnyObjectByType<T>();
        if (found != null)
        {
            _instance._services[serviceType] = found;
            DebugLog.Info($"[ServiceRegistry] Registered {serviceType.Name}");
        }
        else
        {
            DebugLog.LogWarning($"[ServiceRegistry] Service {serviceType.Name} not found in scene");
        }

        return found;
    }

    /// <summary>
    /// Manually registers a service. Call this in Awake() for critical services.
    /// Allows for explicit registration before first Get() call.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="service">Service instance to register</param>
    public static void Register<T>(T service) where T : Component
    {
        if (service == null)
        {
            DebugLog.LogWarning($"[ServiceRegistry] Cannot register null service of type {typeof(T).Name}");
            return;
        }

        if (_instance == null)
        {
            DebugLog.LogWarning($"[ServiceRegistry] No instance exists when registering {typeof(T).Name}");
            GameObject registryGO = new GameObject("ServiceRegistry");
            _instance = registryGO.AddComponent<ServiceRegistry>();
        }

        Type serviceType = typeof(T);
        _instance._services[serviceType] = service;
        DebugLog.Info($"[ServiceRegistry] Manually registered {serviceType.Name}");
    }

    /// <summary>
    /// Clears all cached services. Useful for scene transitions or testing.
    /// </summary>
    public static void Clear()
    {
        if (_instance != null)
        {
            _instance._services.Clear();
            DebugLog.Info("[ServiceRegistry] Cache cleared");
        }
    }

    /// <summary>
    /// Unregisters a specific service type from the cache.
    /// </summary>
    /// <typeparam name="T">Service type to unregister</typeparam>
    public static void Unregister<T>() where T : Component
    {
        if (_instance != null)
        {
            Type serviceType = typeof(T);
            if (_instance._services.Remove(serviceType))
            {
                DebugLog.Info($"[ServiceRegistry] Unregistered {serviceType.Name}");
            }
        }
    }

    /// <summary>
    /// Checks if a service is registered (doesn't trigger FindAnyObjectByType).
    /// </summary>
    /// <typeparam name="T">Service type to check</typeparam>
    /// <returns>True if service is cached</returns>
    public static bool IsRegistered<T>() where T : Component
    {
        if (_instance == null) return false;

        Type serviceType = typeof(T);
        return _instance._services.ContainsKey(serviceType) && _instance._services[serviceType] != null;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            DebugLog.Info("[ServiceRegistry] Instance destroyed, clearing cache");
            _services.Clear();
            _instance = null;
        }
    }
}

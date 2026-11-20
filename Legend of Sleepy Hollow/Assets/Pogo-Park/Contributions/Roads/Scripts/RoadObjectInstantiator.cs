using UnityEngine;

// Script to handle spawning in objects from a list along the roads spilnePath
public class RoadObjectInstantiator : MonoBehaviour
{
    [SerializeField] SplinePath _splinePath;
    [SerializeField] RoadMeshGeneratorStarter _roadMesh;
    public GameObject[] objectsToInstantiate;

    [Header("Placement")]
    [SerializeField, Min(0)] int _objectCount = 1;
    [SerializeField, Min(0f)] float _spacing = 5f;
    [SerializeField, Min(0f)] float _startDistance;
    [SerializeField] bool _alignToRoad = true;
    [SerializeField, Tooltip("When enabled, placement settings and spline point changes refresh the generated copies while playing.")]
    bool _rebuildEveryFrame;

    [Header("Road Offset")]
    [SerializeField] bool _alignToRoadEdge = true;
    [SerializeField] bool _useLeftEdge;
    [SerializeField] bool _spawnOnBothSides;
    [SerializeField, Min(0f)] float _edgeInset = 0.5f;
    [SerializeField] float _distanceFromCenter;

    [Header("Generated Objects")]
    [SerializeField] Transform _objectsParent;
    [SerializeField, HideInInspector] Transform _generatedObjects;

    bool _needsRefresh = true;
    int _lastSettingsHash;

    void OnValidate()
    {
        if (Application.isPlaying)
            _needsRefresh = true;
    }

    void Start()
    {
        ResolveReferences();
        _needsRefresh = true;
        RefreshIfNeeded();
    }

    void Update()
    {
        if (_rebuildEveryFrame && SettingsHash() != _lastSettingsHash)
            _needsRefresh = true;

        RefreshIfNeeded();
    }

    void ResolveReferences()
    {
        if (_splinePath == null)
            _splinePath = GetComponent<SplinePath>();

        if (_roadMesh == null)
            _roadMesh = GetComponent<RoadMeshGeneratorStarter>();
    }

    void RefreshIfNeeded()
    {
        if (!_needsRefresh)
            return;

        _needsRefresh = false;
        RebuildObjects();
        _lastSettingsHash = SettingsHash();
    }

    void RebuildObjects()
    {
        ResolveReferences();
        EnsureGeneratedObjectsParent();
        ClearGeneratedObjects();

        if (_splinePath == null)
        {
            Debug.LogError("RoadObjectInstantiator needs a SplinePath reference.", this);
            return;
        }

        if (objectsToInstantiate == null || objectsToInstantiate.Length == 0)
        {
            Debug.LogWarning("Add at least one prefab to Objects To Instantiate.", this);
            return;
        }

        if (_objectCount <= 0)
            return;

        if (_splinePath.points == null || _splinePath.points.Length < 4 ||
            (_splinePath.points.Length - 1) % 3 != 0)
        {
            Debug.LogError("SplinePath needs 3n + 1 control points before objects can be placed.", this);
            return;
        }

        _splinePath.BuildDistanceTable();
        float pathLength = _splinePath.TotalLength;
        float edgeOffset = Mathf.Abs(_distanceFromCenter);

        if (_alignToRoadEdge)
        {
            if (_roadMesh == null)
            {
                Debug.LogError("Assign the RoadMeshGeneratorStarter to place objects at the road edge.", this);
                return;
            }

            edgeOffset = Mathf.Max(0f, _roadMesh.width * 0.5f - _edgeInset);
            if (!_spawnOnBothSides && _useLeftEdge)
                edgeOffset = -edgeOffset;
        }

        int spawnedCount = 0;
        for (int i = 0; i < _objectCount; i++)
        {
            float distance = _startDistance + i * _spacing;
            if (distance > pathLength)
            {
                Debug.LogWarning($"Only spawned {spawnedCount} instances. The requested count and spacing exceed the spline length.", this);
                break;
            }

            GameObject prefab = objectsToInstantiate[i % objectsToInstantiate.Length];
            if (prefab == null)
            {
                Debug.LogWarning($"Object slot {i} is empty; skipping it.", this);
                continue;
            }

            float parameter = _splinePath.ParameterAtDistance(distance);
            Vector3 center = _splinePath.SamplePoint(parameter);
            Vector3 tangent = _splinePath.SampleTangent(parameter).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
            if (side.sqrMagnitude < 0.001f)
                side = Vector3.right;

            Quaternion rotation = _alignToRoad && tangent.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(tangent, Vector3.up)
                : prefab.transform.rotation;

            if (_spawnOnBothSides)
            {
                Instantiate(prefab, center + side * edgeOffset, rotation, _generatedObjects);
                Instantiate(prefab, center - side * edgeOffset, rotation, _generatedObjects);
                spawnedCount += 2;
            }
            else
            {
                Instantiate(prefab, center + side * edgeOffset, rotation, _generatedObjects);
                spawnedCount++;
            }
        }
    }

    void EnsureGeneratedObjectsParent()
    {
        Transform targetParent = _objectsParent != null ? _objectsParent : transform;
        if (_generatedObjects == null)
        {
            Transform existing = targetParent.Find("Generated Road Objects");
            if (existing != null)
                _generatedObjects = existing;
            else
            {
                GameObject container = new GameObject("Generated Road Objects");
                _generatedObjects = container.transform;
                _generatedObjects.SetParent(targetParent, false);
            }
        }
        else if (_generatedObjects.parent != targetParent)
        {
            _generatedObjects.SetParent(targetParent, false);
        }
    }

    void ClearGeneratedObjects()
    {
        if (_generatedObjects == null)
            return;

        for (int i = _generatedObjects.childCount - 1; i >= 0; i--)
            Destroy(_generatedObjects.GetChild(i).gameObject);
    }

    int SettingsHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (_splinePath != null ? _splinePath.GetInstanceID() : 0);
            hash = hash * 31 + (_roadMesh != null ? _roadMesh.GetInstanceID() : 0);
            hash = hash * 31 + _objectCount;
            hash = hash * 31 + _spacing.GetHashCode();
            hash = hash * 31 + _startDistance.GetHashCode();
            hash = hash * 31 + _alignToRoad.GetHashCode();
            hash = hash * 31 + _rebuildEveryFrame.GetHashCode();
            hash = hash * 31 + _alignToRoadEdge.GetHashCode();
            hash = hash * 31 + _useLeftEdge.GetHashCode();
            hash = hash * 31 + _spawnOnBothSides.GetHashCode();
            hash = hash * 31 + _edgeInset.GetHashCode();
            hash = hash * 31 + _distanceFromCenter.GetHashCode();
            hash = hash * 31 + (_objectsParent != null ? _objectsParent.GetInstanceID() : 0);

            if (objectsToInstantiate != null)
            {
                foreach (GameObject prefab in objectsToInstantiate)
                    hash = hash * 31 + (prefab != null ? prefab.GetInstanceID() : 0);
            }

            if (_roadMesh != null)
                hash = hash * 31 + _roadMesh.width.GetHashCode();

            if (_splinePath != null)
            {
                hash = hash * 31 + _splinePath.samplesPerSegment;
                if (_splinePath.points != null)
                {
                    foreach (Transform point in _splinePath.points)
                        hash = hash * 31 + (point != null ? point.position.GetHashCode() : 0);
                }
            }

            return hash;
        }
    }
}

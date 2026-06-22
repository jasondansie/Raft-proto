using System.Collections.Generic;
using UnityEngine;

namespace RaftProto.Raft
{
    /// <summary>
    /// Integer grid of raft floor tiles in the XZ plane. Stores which cells are occupied
    /// and converts between grid coords and local/world space. In multiplayer the server
    /// will own mutations; clients mirror the result after validation.
    /// </summary>
    public class RaftGrid : MonoBehaviour
    {
        [Header("Grid")]
        [Tooltip("Width and depth of one floor tile in metres. Matches deck cube scale on X/Z.")]
        [SerializeField] private float tileSize = 1f;

        [Header("Bootstrap")]
        [Tooltip("Existing deck objects already in the scene. Registered on Awake.")]
        [SerializeField] private Transform[] initialTileRoots;

        [Header("Debug")]
        [SerializeField] private bool logRegisteredCellsOnAwake;

        private readonly Dictionary<Vector2Int, RaftTile> _tiles = new();

        public float TileSize => tileSize;
        public IReadOnlyDictionary<Vector2Int, RaftTile> Tiles => _tiles;

        private void Awake()
        {
            RegisterInitialTiles();
        }

        public Vector3 CellToLocal(Vector2Int cell)
        {
            float half = tileSize * 0.5f;
            return new Vector3(cell.x * tileSize - half, 0f, cell.y * tileSize - half);
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return transform.TransformPoint(CellToLocal(cell));
        }

        public Vector2Int LocalToCell(Vector3 localPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(localPosition.x / tileSize + 0.5f),
                Mathf.FloorToInt(localPosition.z / tileSize + 0.5f));
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            return LocalToCell(transform.InverseTransformPoint(worldPosition));
        }

        public bool HasTile(Vector2Int cell)
        {
            return _tiles.ContainsKey(cell);
        }

        public bool TryGetTile(Vector2Int cell, out RaftTile tile)
        {
            return _tiles.TryGetValue(cell, out tile);
        }

        public bool RegisterTile(Vector2Int cell, Transform tileRoot)
        {
            if (tileRoot == null)
            {
                Debug.LogWarning($"{nameof(RaftGrid)}: tried to register null tile at {cell}.", this);
                return false;
            }

            if (_tiles.ContainsKey(cell))
            {
                Debug.LogWarning($"{nameof(RaftGrid)}: cell {cell} is already occupied.", this);
                return false;
            }

            _tiles[cell] = new RaftTile(cell, tileRoot);
            return true;
        }

        /// <summary>
        /// Placement rule for new tiles: cell must be empty and orthogonally adjacent to an existing tile.
        /// </summary>
        public bool CanPlaceTile(Vector2Int cell)
        {
            if (HasTile(cell))
            {
                return false;
            }

            if (_tiles.Count == 0)
            {
                return true;
            }

            foreach (Vector2Int neighbor in GetOrthogonalNeighbors(cell))
            {
                if (HasTile(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<Vector2Int> GetOrthogonalNeighbors(Vector2Int cell)
        {
            yield return cell + Vector2Int.up;
            yield return cell + Vector2Int.down;
            yield return cell + Vector2Int.left;
            yield return cell + Vector2Int.right;
        }

        private void RegisterInitialTiles()
        {
            if (initialTileRoots == null)
            {
                return;
            }

            foreach (Transform tileRoot in initialTileRoots)
            {
                if (tileRoot == null)
                {
                    continue;
                }

                Vector2Int cell = LocalToCell(tileRoot.localPosition);
                RegisterTile(cell, tileRoot);

                if (logRegisteredCellsOnAwake)
                {
                    Debug.Log($"{nameof(RaftGrid)}: registered {tileRoot.name} at cell {cell}.", this);
                }
            }
        }
    }

    public readonly struct RaftTile
    {
        public Vector2Int Cell { get; }
        public Transform Root { get; }

        public RaftTile(Vector2Int cell, Transform root)
        {
            Cell = cell;
            Root = root;
        }
    }
}

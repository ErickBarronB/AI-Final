using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridNode
{
    public Vector3 worldPosition;
    public int gridX, gridY;
    public bool isWalkable;
    public float gCost, hCost;
    public float fCost => gCost + hCost;
    public GridNode parent;
    
    public GridNode(Vector3 worldPos, int x, int y, bool walkable)
    {
        worldPosition = worldPos;
        gridX = x;
        gridY = y;
        isWalkable = walkable;
    }
}

public class AStarPlus : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private Vector2 gridSize = new Vector2(50, 50);
    [SerializeField] private float nodeRadius = 0.5f;
    [SerializeField] private LayerMask obstacleLayerMask = 1;
    
    [Header("Debug")]
    [SerializeField] private bool showGridDebug = false;
    
    private GridNode[,] grid;
    private float nodeDiameter;
    private int gridCountX, gridCountY;
    
    public static AStarPlus Instance { get; private set; }
    public Vector2 GridSize => gridSize;
    public Vector3 GridCenter => transform.position;
    
    public bool IsPositionInGrid(Vector3 position)
    {
        Vector3 gridCenter = transform.position;
        float halfSizeX = gridSize.x / 2f;
        float halfSizeZ = gridSize.y / 2f;
        
        return position.x >= gridCenter.x - halfSizeX &&
               position.x <= gridCenter.x + halfSizeX &&
               position.z >= gridCenter.z - halfSizeZ &&
               position.z <= gridCenter.z + halfSizeZ;
    }
    
    public Vector3 ClampToGrid(Vector3 position)
    {
        Vector3 gridCenter = transform.position;
        float halfSizeX = gridSize.x / 2f;
        float halfSizeZ = gridSize.y / 2f;
        
        return new Vector3(
            Mathf.Clamp(position.x, gridCenter.x - halfSizeX, gridCenter.x + halfSizeX),
            position.y,
            Mathf.Clamp(position.z, gridCenter.z - halfSizeZ, gridCenter.z + halfSizeZ)
        );
    }
    
    void Awake()
    {
        Instance = this;
        nodeDiameter = nodeRadius * 2;
        gridCountX = Mathf.RoundToInt(gridSize.x / nodeDiameter);
        gridCountY = Mathf.RoundToInt(gridSize.y / nodeDiameter);
        CreateGrid();
    }
    
    void CreateGrid()
    {
        grid = new GridNode[gridCountX, gridCountY];
        Vector3 bottomLeft = transform.position - Vector3.right * gridSize.x / 2 - Vector3.forward * gridSize.y / 2;
        
        for (int x = 0; x < gridCountX; x++)
        {
            for (int y = 0; y < gridCountY; y++)
            {
                Vector3 worldPoint = bottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                worldPoint.y = 0f;
                
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, obstacleLayerMask);
                grid[x, y] = new GridNode(worldPoint, x, y, walkable);
            }
        }
    }
    
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        GridNode startNode = GetNodeFromWorldPosition(startPos);
        GridNode targetNode = GetNodeFromWorldPosition(targetPos);
        
        if (startNode == null || targetNode == null || !targetNode.isWalkable)
            return new List<Vector3>();
        
        List<GridNode> openSet = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        openSet.Add(startNode);
        
        while (openSet.Count > 0)
        {
            GridNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || 
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode)
            {
                return CleanPath(RetracePath(startNode, targetNode));
            }
            
            foreach (GridNode neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                    continue;
                
                float newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;
                    
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        
        return new List<Vector3>();
    }
    
    List<Vector3> RetracePath(GridNode startNode, GridNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        GridNode currentNode = endNode;
        
        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }
        
        path.Reverse();
        return path;
    }
    
    List<Vector3> CleanPath(List<Vector3> originalPath)
    {
        if (originalPath.Count <= 2) return originalPath;
        
        List<Vector3> cleanedPath = new List<Vector3> { originalPath[0] };
        
        for (int i = 1; i < originalPath.Count - 1; i++)
        {
            Vector3 directionToNext = (originalPath[i + 1] - originalPath[i - 1]).normalized;
            Vector3 directionCurrent = (originalPath[i] - originalPath[i - 1]).normalized;
            
            if (Vector3.Dot(directionToNext, directionCurrent) < 0.9f || 
                Physics.Linecast(originalPath[i - 1], originalPath[i + 1], obstacleLayerMask))
            {
                cleanedPath.Add(originalPath[i]);
            }
        }
        
        cleanedPath.Add(originalPath[originalPath.Count - 1]);
        return cleanedPath;
    }
    
    GridNode GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridSize.x / 2) / gridSize.x;
        float percentY = (worldPosition.z + gridSize.y / 2) / gridSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        
        int x = Mathf.RoundToInt((gridCountX - 1) * percentX);
        int y = Mathf.RoundToInt((gridCountY - 1) * percentY);
        
        if (x >= 0 && x < gridCountX && y >= 0 && y < gridCountY)
            return grid[x, y];
        
        return null;
    }
    
    List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                
                if (checkX >= 0 && checkX < gridCountX && checkY >= 0 && checkY < gridCountY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        
        return neighbors;
    }
    
    float GetDistance(GridNode nodeA, GridNode nodeB)
    {
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        
        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);
        return 14 * distX + 10 * (distY - distX);
    }
    
    void OnDrawGizmos()
    {
		if (!showGridDebug) return;
		
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(transform.position, new Vector3(gridSize.x, 1, gridSize.y));
		
		if (grid != null)
		{
			foreach (GridNode node in grid)
			{
				Gizmos.color = node.isWalkable ? Color.white : Color.red;
				Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
			}
		}
    }
}

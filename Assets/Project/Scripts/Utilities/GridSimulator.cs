using UnityEngine;

public struct Coordinate 
{
    public int row { get; set; }
    public int col { get; set; }

    public Coordinate(int row, int col)
    {
        this.row = row;
        this.col = col;
    }
}

public struct GridInitializer
{
    public float mapWidth { get; set; }
    public float mapHeight { get; set; }
    public int numRegionColumns { get; set; }
    public int numRegionRows { get; set; }
    public int numCellColumns { get; set; }
    public int numCellRows { get; set; }
}

// ------------------------------------------------------------- //
//    A class that fakes a grid manager, it only has helper
//    functions that cast indices, but no actual grid data
// ------------------------------------------------------------- //
public class GridSimulator
{
    // ------------------- //
    //      Variables
    // ------------------- //
    public float cellDimension { get; }
    public float regionDimension { get; }
    public float mapWidth { get; }
    public float mapHeight { get; }
    public int numCellColumns { get; }
    public int numCellRows { get; }
    public int numRegionColumns { get; }
    public int numRegionRows { get; }

    public GridSimulator(GridInitializer initializer)
    {
        mapWidth = initializer.mapWidth;
        mapHeight = initializer.mapHeight;
        numRegionColumns = initializer.numRegionColumns;
        numRegionRows = initializer.numRegionRows;
        numCellColumns = initializer.numCellColumns;
        numCellRows = initializer.numCellRows;
        // region and cell size in pixels, we use square region and cell
        regionDimension = mapWidth / numRegionColumns;
        cellDimension = mapWidth / numCellColumns; 
    }

    // -------------------------------- //
    //          Public Methods
    // -------------------------------- //
    // casting helper
    public Coordinate IndexToCoordinate(int index)
    {
        return new Coordinate(index / numCellColumns, index % numCellColumns);
    }

    public int CoordinateToIndex(Coordinate coord)
    {
        return coord.row * numCellColumns + coord.col;
    }
    
    public int CoordinateToIndex(int row, int col)
    {
        return row * numCellColumns + col;
    }

    public Vector3 IndexToWorldPositionCentered(int index)
    {
        Coordinate coord = IndexToCoordinate(index);

        return CoordinateToWorldPositionCentered(coord);
    }
    
    public Vector3 CoordinateToWorldPositionCentered(Coordinate coord)
    {
        float WorldX = coord.col * cellDimension + (cellDimension / 2.0f) - (mapWidth / 2.0f) ;
        float worldZ = (coord.row * cellDimension + (cellDimension / 2.0f) - (mapHeight / 2.0f)) * -1.0f;

        return new Vector3(WorldX, 0f, worldZ);
    }

    public Vector3 CoordinateToWorldPositionCentered(int row, int col)
    {
        float WorldX = col * cellDimension + (cellDimension / 2.0f) - (mapWidth / 2.0f) ;
        float worldZ = (row * cellDimension + (cellDimension / 2.0f) - (mapHeight / 2.0f)) * -1.0f;

        return new Vector3(WorldX, 0f, worldZ);
    }

    // cast world position to board index, return -1 when index not valid
    public int WorldPositionToIndex(Vector3 worldPos)
    {
        float boardX = worldPos.x + (mapWidth / 2.0f);
        float boardZ = Mathf.Abs(worldPos.z - (mapHeight / 2.0f));

        int row = (int)(boardZ / cellDimension);
        int col = (int)(boardX / cellDimension);
        
        if (row >= 0 && row < numCellRows && col >= 0 && col < numCellColumns)
            return row * numCellColumns + col;     

        return -1;   
    }

    // Find which region the index lies into. Return region id if index is valid, otherwise return -1;
    public int CoordinateToRegion(Coordinate coord)
    {
        // check for validation
        if (!IsValidCell(coord))
            return -1;

        int regionRow = coord.row  / (int)(regionDimension / cellDimension);
        int regionCol = coord.col  / (int)(regionDimension / cellDimension);

        return regionRow * numRegionColumns + regionCol;
    }

    public int CoordinateToRegion(int row, int col)
    {
        // check for validation
        if (!IsValidCell(row, col))
            return -1;

        int regionRow = row  / (int)(regionDimension / cellDimension);
        int regionCol = col  / (int)(regionDimension / cellDimension);

        return regionRow * numRegionColumns + regionCol;
    }

    public int IndexToRegion(int index)
    {
        Coordinate coord = IndexToCoordinate(index);

        return CoordinateToRegion(coord);
    }

    public int WorldPositionToRegion(Vector3 worldPos)
    {
        float boardX = worldPos.x + (mapWidth / 2.0f);
        float boardZ = Mathf.Abs(worldPos.z - (mapHeight / 2.0f));

        int regionRow = (int)(boardZ / regionDimension);
        int regionCol = (int)(boardX / regionDimension);

        if (IsValidRegion(regionRow, regionCol))
            return regionRow * numRegionColumns + regionCol;  

        return -1;
    }

    public bool IsValidCell(int cellIndex)
    {
        return cellIndex >= 0 && cellIndex < numCellColumns * numCellRows;
    }

    public bool IsValidCell(Coordinate coord)
    {
        return coord.row >= 0 && coord.row < numCellRows && coord.col >=0 && coord.col < numCellColumns;
    }

    public bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < numCellRows && col >=0 && col < numCellColumns;
    }

    public bool IsValidRegion(int regionIndex)
    {
        return regionIndex >= 0 && regionIndex < numRegionColumns * numRegionRows;
    }

    public bool IsValidRegion(int regionRow, int regionCol)
    {
        return regionRow >= 0 && regionRow < numRegionRows && regionCol >=0 && regionCol < numRegionColumns; 
    }
}
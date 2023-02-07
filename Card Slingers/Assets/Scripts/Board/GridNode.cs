public class GridNode
{
    private FieldGrid<GridNode> _fieldGrid;
    public FieldGrid<GridNode> Grid => _fieldGrid;

    public int x { get; private set; }
    public int y { get; private set; }

    public bool isOccupied { get; private set; }

    public GridNode(FieldGrid<GridNode> grid, int x, int y)
    {
        _fieldGrid = grid;
        this.x = x;
        this.y = y;

        isOccupied = false;
    }

    //Toggle whether the node is occupied by an item
    public void SetOccupied(bool isOccupied)
    {
        this.isOccupied = isOccupied;
        _fieldGrid.TriggerGridObjectChanged(x, y);
    }

    public override string ToString()
    {
        return x + "," + y;
    }
}

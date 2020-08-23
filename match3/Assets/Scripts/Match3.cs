using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match3 : MonoBehaviour
{
    

    [Header("UI Elements")]
    public Sprite[] pieces;
    public RectTransform gameBoard;
    public RectTransform fallenBoard;

    [Header("Prefabs")]
    public GameObject nodePiece;
    public GameObject fallenPiece;

    int width = 7;
    int height = 7;
    int[] fills;
    public ArrayLayout boardLayout;

    Node[,] board;

    List<NodePiece> update;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;
    List<FallingPiece> fallen;

    System.Random random;


    // Start is called before the first frame update
    void Start()
    {
        StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for(int i = 0; i < update.Count; i++)
        {
            NodePiece piece = update[i];
            if (!piece.UpdatePiece()) finishedUpdating.Add(piece);

        }
        for (int i = 0; i < finishedUpdating.Count; i++)
        {
            NodePiece piece = finishedUpdating[i];
            FlippedPieces flip = getFlipped(piece);
            NodePiece flippedPiece = null;
            
            int x = (int)piece.index.x;
            fills[x] = Mathf.Clamp(fills[x] -1, 0 ,width);

            List<Point> connected = isConnected(piece.index, true);
            bool wasFlipped = (flip != null);

            if (wasFlipped) // if we flipped to make this update
            {
                flippedPiece = flip.getOtherPiece(piece);
                AddPoints(ref connected, isConnected(flippedPiece.index, true));
            }

            if (connected.Count == 0) // if we didn't make a match
            {
                if (wasFlipped) // if we flipped 
                    FlipPieces(piece.index, flippedPiece.index, false); // flip back
            }
            else // If we made a match
            {
                foreach(Point pnt in connected) // Remove the node pieces connected
                {
                    FallPiece(pnt);
                    Node node = getNodeAtPoint(pnt);
                    NodePiece nodePiece = node.getPiece();
                    if(nodePiece != null)
                    {
                        nodePiece.gameObject.SetActive(false);
                        dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }
                ApplyGravityToBoard();
            }
            flipped.Remove(flip); //remove the flip after update
            update.Remove(piece);
        }
    } 

    void ApplyGravityToBoard()
    {
        for(int x = 0; x  < width; x++)
        {
            for(int y = (height-1); y >= 0; y--)
            {
                Point p = new Point(x, y);
                Node node = getNodeAtPoint(p);
                int val = getValueAtPoint(p);
                if (val != 0) continue; //If it is not a hole, do nothing
                for(int ny = (y-1); ny >= -1; ny--)
                {
                    Point next = new Point(x, ny);
                    int nextVal = getValueAtPoint(next);
                    if (nextVal == 0)
                        continue;
                    if (nextVal != -1) // If we did not hit an end, but it is not 0 then use this to fill the current hole
                    {
                        Node got = getNodeAtPoint(next);
                        NodePiece piece = got.getPiece();

                        // Set the hole
                        node.SetPiece(piece);
                        update.Add(piece);

                        // Make a new hole
                        got.SetPiece(null); 
                    }
                    else // Hit an end. Use dead ones or create new pieces to fill holes (hit a -1) only if we choose to
                    {
                        // Fill in the hole
                        int newVal = fillPiece();
                        NodePiece piece;
                        Point fallPnt = new Point(x, (-1 - fills[x]));
                        if (dead.Count > 0) // Check if there is any dead pieces
                        {
                            // Revive dead piece
                            NodePiece revived = dead[0];
                            revived.gameObject.SetActive(true);
                            piece = revived;
                            
                            dead.RemoveAt(0); // remove from the dead list
                        }
                        else
                        {
                            // Should not be called if your whole board is already filled
                            // Called unless some special power broke pieces without connecting the pieces to fill the board 
                            GameObject obj = Instantiate(nodePiece, gameBoard);
                            NodePiece n = obj.GetComponent<NodePiece>();
                            RectTransform rect = obj.GetComponent<RectTransform>();
                            piece = n;
                        }
                        piece.Initialize(newVal, p, pieces[newVal - 1]); // Set revived piece to new value at p (position of the hole)
                        piece.rect.anchoredPosition = getPositionFromPoint(fallPnt);// -1 makes the piece fall from top to bottom

                        Node hole = getNodeAtPoint(p);
                        hole.SetPiece(piece);
                        ResetPiece(piece);
                        fills[x]++;
                    }
                    
                    break;
                }
            }
        }
    }

    FlippedPieces getFlipped(NodePiece p)
    {
        FlippedPieces flip = null;
        for (int i = 0; i < flipped.Count; i++)
        { 
            if (flipped[i].getOtherPiece(p) != null)
            {
                flip = flipped[i];
                break;
            }
        }
        return flip;
    }

    void StartGame()
    {

        fills = new int[width];
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        update = new List<NodePiece>();
        flipped = new List<FlippedPieces>();
        dead = new List<NodePiece>();
        fallen = new List<FallingPiece>();
        InitializeBoard();
        VerifyBoard();
        InstantiateBoard();
    }

    void InitializeBoard()
    {
        board = new Node[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // WHEN THE BOARDLAYOUT IS CLICKED, IT WILL BE FILLED 
                board[x, y] = new Node(boardLayout.rows[y].row[x] ? -1 : fillPiece(), new Point(x, y));
            }
        }
    }

    void VerifyBoard()
    {
        List<int> remove;
        for (int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                Point p = new Point(x, y);
                int val = getValueAtPoint(p);
                if (val <= 0) continue;

                remove = new List<int>(); 
                while(isConnected(p, true).Count > 0)
                {
                    val = getValueAtPoint(p);
                    if (!remove.Contains(val))
                        remove.Add(val);
                    setValueAtPoint(p, newValue(ref remove));
                }

            }
        }
    }

    void InstantiateBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = getNodeAtPoint(new Point(x, y));
                int val = node.value;
                if (val <= 0) continue;
                GameObject p = Instantiate(nodePiece, gameBoard);
                NodePiece piece = p.GetComponent<NodePiece>();
                RectTransform rect = p.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(32 + (64 * x), -32 - (64 * y));
                piece.Initialize(val, new Point (x,y), pieces[val - 1]);
                node.SetPiece(piece);
            }
        }
    }

    public void ResetPiece(NodePiece piece) // RESET PIECE BACK TO ORIGINAL POSITIOn
    {
        piece.ResetPosition();
        update.Add(piece);
    }

    public void FlipPieces(Point one, Point two, bool main)
    {
        if (getValueAtPoint(one) < 0) return;
        Node nodeOne = getNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.getPiece();

        if (getValueAtPoint(two) > 0)
        {
            Node nodeTwo = getNodeAtPoint(two);
            NodePiece pieceTwo = nodeTwo.getPiece();
            nodeOne.SetPiece(pieceTwo);
            nodeTwo.SetPiece(pieceOne);

            if (main)
                flipped.Add(new FlippedPieces(pieceOne, pieceTwo));

            update.Add(pieceOne);
            update.Add(pieceTwo);
        }
        else
            ResetPiece(pieceOne);
    }

    void FallPiece(Point p)
    {
        List<FallingPiece> available = new List<FallingPiece>();
        for (int i = 0;  i < fallen.Count; i++)
        {
            if (!fallen[i].falling) available.Add(fallen[i]);

        }
        FallingPiece set = null;
        if (available.Count > 0)
            set = available[0];
        else
        {
            GameObject fell = GameObject.Instantiate(fallenPiece, fallenBoard);
            FallingPiece fPiece = fell.GetComponent<FallingPiece>();
            set = fPiece;
            fallen.Add(fPiece);
        }
        int val = getValueAtPoint(p) - 1;
        if(set != null && val >= 0 && val < pieces.Length)
        {
            set.Initialize(pieces[val], getPositionFromPoint(p));
        }
    }

    List<Point> isConnected(Point p, bool main)
    {
        List<Point> connected = new List<Point>();
        int val = getValueAtPoint(p);
        Point[] directions =
        {
            Point.up,
            Point.right,
            Point.down,
            Point.left,
        };

        foreach(Point dir in directions) // Checking if there is 2 or more same shapes in the directions Ex: OXOOXO
        {
            List<Point> line = new List<Point>();

            int same = 0;
            for(int i= 1; i < 3; i++)
            {
                Point check = Point.add(p, Point.mult(dir, i)); 
                if(getValueAtPoint(check) == val)
                {
                    line.Add(check);
                    same++;
                }
            }
            if (same > 1) // If there are more than 1 of the same shape in the direction then we know it is a match 
                AddPoints(ref connected, line); // add these points to the overarching connected list
        }

        for (int i = 0; i < 2; i++) // Check if we are in the middle of two of the same shapes
        {
            List<Point> line = new List <Point>();
            int same = 0;
            // {Check Point.up and Point.right, Check Point.down and Point.left}
            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[i + 2]) };
            foreach (Point next in check) // CHeck both sides of the piece, if they are the same value, add them to the list
            {
                if (getValueAtPoint(next) == val)
                {
                    line.Add(next);
                    same++;
                }
            }
            if(same > 1)
            {
                AddPoints(ref connected, line);
            }
        }

        for(int i = 0; i < 4; i++) // checks for a 2x2
        {
            List<Point> square = new List<Point>();

            int same = 0;
            int next = i + 1;
            if (next >= 4)
            {
                next -= 4;
            }
            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[next]), Point.add(p, Point.add(directions[i], directions[next])) };
            foreach (Point pnt in check) // CHeck all sides of the piece, if they are the same value, add them to the list
            {
                if (getValueAtPoint(pnt) == val)
                {
                    square.Add(pnt);
                    same++;
                }
            }
            if (same > 2)
                AddPoints(ref connected, square);
        }

        if (main) // Checks for other matches along the current match
        {
            for(int i = 0; i < connected.Count; i++)
            {
                AddPoints(ref connected, isConnected(connected[i], false));
            }
        }

        return connected; 

    }

    void AddPoints(ref List<Point> points, List<Point> add)
    {
        foreach(Point p in add)
        {
            bool doAdd = true;
            for(int i = 0; i < points.Count; i++)
            {
                if (points[i].Equals(p))
                {
                    doAdd = false;
                    break;
                }
            }

            if (doAdd)
            {
             points.Add(p);
            }
               
        }
    }

    int fillPiece()
    {
        int val = 1; //Start value is 1 because we set cube as 1
        // Choose Cube/Sphere/Diamond etc and put the piece in the board
        val = (random.Next(0, 100) / (100 / pieces.Length)) + 1; 
        return val;
    }

    int getValueAtPoint(Point p)
    {
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) return -1;
        return board[p.x, p.y].value;
    }

    void setValueAtPoint(Point p, int v)
    {
        board[p.x, p.y].value = v;
    }

    Node getNodeAtPoint(Point p)
    {
        return board[p.x, p.y];
    }

    int newValue(ref List<int> remove)
    {
        List<int> available = new List<int>();
        for (int i = 0; i< pieces.Length; i++)
        {
            available.Add(i + 1);
        }
        foreach(int i in remove)
        {
            available.Remove(i);
        }
        if (available.Count <= 0) return 0;
        return available[random.Next(0, available.Count)];
    }
   

    string getRandomSeed()
    {
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()";
        for (int i = 0; i < 20; i++)
        {
            seed += acceptableChars[Random.Range(0, acceptableChars.Length)];
        }
        return seed;
    }

    public Vector2 getPositionFromPoint(Point p)
    {
        return new Vector2(32 + (64 * p.x), -32 - (64 * p.y));
    }
}

[System.Serializable]
public class Node
{
    public int value; // 0 = blank, 1 = cube, 2 = sphere, 3 = cylinder, 4 = pyramid, 5 = diamond, -1 = hole
    public Point index;
    NodePiece piece;

    public Node(int v, Point i)
    {
        value = v;
        index = i;
    }
    public void SetPiece(NodePiece p)
    {
        piece = p;
        value = (piece == null) ? 0 : piece.value;
        if (piece == null) return;
        piece.SetIndex(index);
    }
    public NodePiece getPiece()
    {
        return piece; 
    }

}

[System.Serializable]
public class FlippedPieces
{
    public NodePiece one;
    public NodePiece two;

    public FlippedPieces(NodePiece o, NodePiece t)
    {
        one = o;
        two = t;
    }
    public NodePiece getOtherPiece(NodePiece p)
    {
        if (p == one)
            return two;
        else if (p == two)
            return one;
        else
            return null;
    }
}

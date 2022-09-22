using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName ="New CellType",menuName ="Cells/New Cell type")]
public class Cell : ScriptableObject
{
//Set in the Unity ScriptableObject editor
    public Sprite baseSprite;
    public static Sprite sprite;
    public Color CellColor;
    public int requiredNeighborsForUnderPopDeath;
    public int requiredNeighborsForOverPopDeath;
    public int requiredNeighborsForReproduction;
    public bool carnivoreCell;
    public string id;
    public KeyCode hotKey;
    public bool ages = false;
    public int maxAgeInTicks = 100;
    
    //Stores tiles
    private Dictionary<int,Tile> tiles = new Dictionary<int,Tile>();
    
    //Generate tiles, etc
    public void Setup()
    {
        if (sprite == null)
            sprite = baseSprite;
        if (!ages)
        {
            Tile tile = (Tile)CreateInstance(typeof(Tile));
            tile.sprite = baseSprite;
            tile.color = CellColor;
            tiles.Add(0,tile);
        }
        else
        {
            for (int x = 255; x > 0; x -= (255 / maxAgeInTicks))
            {
                tiles.Add(x, new Tile()
                {
                    color = new Color(CellColor.r, CellColor.g, CellColor.b, x)
                });
            }
        }
    }
    
    public Tile GetTile(int age)
    {
        if (!ages)
        {

        }
        return tiles[0];
    }

    public bool Eaten(Cell[,] map, Vector2Int position, int gameBoardSize, out Cell eater)
    {
        for (int xPos = position.x - 1; xPos <= position.x + 1; xPos++)
        {
            for (int yPos = position.y - 1; yPos <= position.y + 1; yPos++)
            {
                //Wrap board
                int x = xPos;
                int y = yPos;
                if (x >= gameBoardSize)
                    x = 0;
                if (x < 0)
                    x = gameBoardSize - 1;
                if (y < 0)
                    y = gameBoardSize - 1;
                if (y >= gameBoardSize)
                    y = 0;

                if (!(y == position.y && x == position.x))
                {
                    if (map[x, y] != null)
                        if (map[x, y].carnivoreCell)
                        {
                            eater = map[x, y];
                            bool eaten = Random.Range(1, 4) == 3 && eater != this;

                            if (eaten)
                                map[x, y] = eater;
                            return eaten;
                        }
                }
            }
        }
        eater=null;
        return false;
    }

    public int GetNeighbors(Cell[,] map, Vector2Int position,int gameBoardSize)
    {
        float amount = 0;
        //Move in a 3x3 space around the center to find all possible neighbors
        for (int xPos = position.x - 1; xPos <= position.x + 1; xPos++)
        {
            for (int yPos = position.y - 1; yPos <= position.y + 1; yPos++)
            {
                //Wrap board
                int x = xPos;
                int y = yPos;
                if (x >= gameBoardSize)
                    x = 0;
                if (x < 0)
                    x = gameBoardSize - 1;
                if (y < 0)
                    y = gameBoardSize - 1;
                if (y >= gameBoardSize)
                    y = 0;

                    //Make sure it is not the center cell
                    if (!(y == position.y && x == position.x))
                    {
                        if (map[x, y] != null)
                        {
                            if (!carnivoreCell)
                                amount += map[x, y] == this ? 1 : 0;
                            else
                                amount += map[x,y]==this? 1 : 0;
                        }
                    }
            }
        }

        return Mathf.RoundToInt(amount);
    }

    public bool Dies(Cell[,] map, Vector2Int position, int gameBoardSize)
    {
        //Get all neighbors.
        int neighbors = GetNeighbors(map, position, gameBoardSize);
        return neighbors > requiredNeighborsForOverPopDeath || GetNeighbors(map,position,gameBoardSize)<requiredNeighborsForUnderPopDeath || (Eaten(map,position,gameBoardSize,out Cell eater));
    }

    public bool ShouldBePlacedHere(Cell[,] map, Vector2Int position, int gameBoardSize, out bool eats)
    {
        //Get all neighbors.
        int neighbors = GetNeighbors(map, position, gameBoardSize);
        eats = false;
        //Do we eat a cell here?
        if (carnivoreCell)
        {
            eats = map[position.x, position.y] != null;
        }
        return requiredNeighborsForReproduction == neighbors;
    }
}

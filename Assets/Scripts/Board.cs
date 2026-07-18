using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = System.Random;

/// <summary>
/// coder by shlifedev(zero is black)
/// </summary>
public class Board : MonoBehaviour
{
    public enum State
    {
        WAIT, PROCESSING, END
    }

    public State state = State.WAIT;
    public static Board Instance
    {
        get
        {
            if (_inst == null) _inst = FindObjectOfType<Board>();
            return _inst;
        }
    } 
    private static Board _inst; 
    public List<NodeObject> realNodeList = new List<NodeObject>(); 
    public List<Node> nodeData = new List<Node>();
    public Dictionary<Vector2Int, Node> nodeMap = new Dictionary<Vector2Int, Node>();
    public int col = 4;
    public int row = 4;
    public GameObject emptyNodePrefab;
    public GameObject nodePrefab;
    public RectTransform emptyNodeRect;
    public RectTransform realNodeRect;

    public int score = 0;                     // 当前得分
    public int bestScore = 0;                 // 最高分
    public TextMeshProUGUI scoreText;         // 用来显示当前分数的UI文字（注意用TMP）
    public TextMeshProUGUI bestScoreText;     // 用来显示最高分的UI文字

    // --- 新增计时器变量 ---
    public TextMeshProUGUI timerText;    // 用来显示时间的UI文字
    private float elapsedTime = 0f;      // 累积经过的时间（秒）
    private bool isTimerRunning = false; // 控制计时器是否走动

    public GameObject gameOverPanel;        // 拖入 GameOverPanel 物体
    public TextMeshProUGUI finalScoreText;  // 拖入 FinalScoreText
    public TextMeshProUGUI finalTimeText;   // 拖入 FinalTimeText

    private bool isGameOver = false;        // 是否已结束（防止重复触发）
    public void OnGameOver()
    {
        if (isGameOver) return; // 防止重复弹窗
        isGameOver = true;

        isTimerRunning = false; // 停止计时

        // 显示面板并更新最终数据
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreText != null) finalScoreText.text = "Score: " + score;
            if (finalTimeText != null) finalTimeText.text = "Time: " + timerText.text;
        }
        Debug.Log("Game Over!!!!");
    }
    private void CreateBoard()
    {
        /* first initialize empty Node rect*/
        realNodeList.Clear();
        nodeMap.Clear();
        nodeData.Clear();
        var emptyChildCount = emptyNodeRect.transform.childCount;
        for (int i = 0; i < emptyChildCount; i++)
        {
            var child = emptyNodeRect.GetChild(i);
        }

        /* and, empty node create for get grid point*/
        for (int i = 0; i < col; i++)
        {
            for (int j = 0; j < row; j++)
            {
                var instantiatePrefab = GameObject.Instantiate(emptyNodePrefab, emptyNodeRect.transform, false);  
                var point = new Vector2Int(j, i);
                //r-d-l-u
                Vector2Int left = point - new Vector2Int(1, 0); 
                Vector2Int down = point - new Vector2Int(0, 1); 
                Vector2Int right = point + new Vector2Int(1, 0);
                Vector2Int up = point + new Vector2Int(0, 1); 
                Vector2Int?[] v = new Vector2Int?[4];
                if (IsValid(right)) v[0] = right;
                if (IsValid(down)) v[1] = down;
                if (IsValid(left)) v[2] = left;
                if (IsValid(up)) v[3] = up; 
                Node node = new Node(v);
                node.point = point;
                node.nodeRectObj = instantiatePrefab; 
                nodeData.Add(node);
                instantiatePrefab.name = node.point.ToString();
                this.nodeMap.Add(point, node);
            }
        }
        /* grid 정렬 */
        LayoutRebuilder.ForceRebuildLayoutImmediate(emptyNodeRect);
        foreach (var data in nodeData)
            data.position = data.nodeRectObj.GetComponent<RectTransform>().localPosition;


    }

    private bool IsValid(Vector2Int point)
    {
        if (point.x == -1 || point.x == row || point.y == col || point.y == -1)
            return false;

        return true;
    } 
    private void CreateBlock(int x, int y)
    {
        if (nodeMap[new Vector2Int(x, y)].realNodeObj != null) return;
        
        GameObject realNodeObj = Instantiate(nodePrefab, realNodeRect.transform, false);
        var node = nodeMap[new Vector2Int(x, y)];
        var pos = node.position; 
        realNodeObj.GetComponent<RectTransform>().localPosition = pos; 
        realNodeObj.transform.DOPunchScale(new Vector3(.32f, .32f, .32f), 0.15f, 3);
        var nodeObj = realNodeObj.GetComponent<NodeObject>();
        this.realNodeList.Add(nodeObj);
        nodeObj.InitializeFirstValue();
        node.value = nodeObj.value;
        node.realNodeObj = nodeObj; 
    }
 
    public void Combine(Node from, Node to)
    {
     //   Debug.Log($"TRY COMBINE {from.point} , {to.point}");
        to.value = to.value * 2;

        // 1. 累加得分（合并后的值就是得分）
        score += to.value.Value;   // 因为 to.value 是 int? 类型，用 .Value 取值

        // 2. 更新最高分并保存到本地
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore); // 存入本地
            PlayerPrefs.Save();                         // 立即写入磁盘
        }

        // 3. 更新UI文字显示（如果UI绑定好了）
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (bestScoreText != null) bestScoreText.text = "Best: " + bestScore;

        from.value = null;
        if (from.realNodeObj != null)
        {
            from.realNodeObj.CombineToNode(from, to);
            from.realNodeObj = null;
            to.combined = true;
        }
    }

    public void Move(Node from, Node to)
    {
//        Debug.Log($"TRY MOVE {from.point} , {to.point}");
        to.value = from.value;
        from.value = null;  
        if (from.realNodeObj != null)
        {
            from.realNodeObj.MoveToNode(from, to);
            if (from.realNodeObj != null)
            {
                to.realNodeObj = from.realNodeObj;
                from.realNodeObj = null;
                Debug.Log(to.realNodeObj != null);
            }
        }
    }
 
    /// <summary>
    /// Move Blocks by User Input.
    /// </summary>
    /// <param name="dir"></param>
    public void MoveTo(Node.Direction dir)
    {
        if (dir == Node.Direction.RIGHT)
        {
            for (int j = 0; j < col; j++)
            {
                for (int i = (row - 2); i >= 0; i--)
                { 
                    var node = nodeMap[new Vector2Int(i, j)];
                    if (node.value == null) 
                        continue; 
                    var right = node.FindTarget(node, Node.Direction.RIGHT);
                    if (right != null)
                    {
                        if (node.value.HasValue && right.value.HasValue)
                        {
                            if (node.value == right.value)
                            {
                                Combine(node, right);
                            }
                        }
                        else if (right != null && right.value.HasValue == false)
                        {
                             Move(node, right);
                        } 
                        else if (right == null)
                            return;
                    } 
                }
            }
 
        }
        if (dir == Node.Direction.LEFT)
        { 
            for (int j = 0; j< col; j ++)
            {
                for (int i = 1; i < row; i++)
                { 
                    var node = nodeMap[new Vector2Int(i, j)];
                    if (node.value == null) 
                        continue; 

                    var left = node.FindTarget(node, Node.Direction.LEFT);
                    if (left != null)
                    {
                        if (node.value.HasValue && left.value.HasValue)
                        {
                            if (node.value == left.value)
                            {
                                Combine(node, left);
                            }
                        }
                        else if (left != null && left.value.HasValue == false)
                        {
                            Move(node, left);
                        }  
                    } 
                }
            }
 
        }
        if (dir == Node.Direction.UP)
        {
            for (int j = col-2; j >= 0; j --)
            {
                for (int i = 0; i<row; i++)
                {
                    var node = nodeMap[new Vector2Int(i, j)];
                    if (node.value == null) 
                        continue;  
                    var up = node.FindTarget(node, Node.Direction.UP);
                    if (up != null)
                    {
                        if (node.value.HasValue && up.value.HasValue)
                        {
                            if (node.value == up.value)
                            {
                                Combine(node, up);
                            }
                        }
                        else if (up != null && up.value.HasValue == false)
                        {
                            Move(node, up);
                        }  
                    } 
                }
            }
        }
        if (dir == Node.Direction.DOWN)
        {
            for (int j = 1; j<col; j++)
            {
                for (int i = 0; i< row; i++)
                {
                    var node = nodeMap[new Vector2Int(i, j)];
                    if (node.value == null) 
                        continue;  
                    var down = node.FindTarget(node, Node.Direction.DOWN);
                    if (down != null)
                    {
                        if (node.value.HasValue && down.value.HasValue)
                        {
                            if (node.value == down.value)
                            {
                                Combine(node, down);
                            }
                        }
                        else if (down != null && down.value.HasValue == false)
                        {
                            Move(node, down);
                        }  
                    } 
                }
            }
        }
        
        foreach (var data in realNodeList)
        {
            if (data.target != null)
            {
                state = State.PROCESSING; 
                data.StartMoveAnimation(); 
            }
        }
        
        Show();
        if (IsGameOver())
        {
           OnGameOver();
        }
    }

    /// <summary>
    /// if can't combine anymore then game over!!!!
    /// </summary>
    /// <returns></returns>
    public bool IsGameOver()
    {
        bool gameOver = true;
        nodeData.ForEach(x =>
        { 
            for (int i = 0; i < x.linkedNode.Length; i++)
            {
                if (x.realNodeObj == null) gameOver = false;
                if (x.linkedNode[i] == null)
                    continue;
                
                var nearNode = nodeMap[x.linkedNode[i].Value];
                if(x.value != null && nearNode.value != null)
                if (x.value == nearNode.value)
                {
                    gameOver = false;
                }
            } 
        });

        return gameOver;
    }
    private void CreateRandom()
    {
        var emptys = nodeData.FindAll(x => x.realNodeObj == null); 
        if (emptys.Count == 0)
        {
            if (IsGameOver())
            {
                OnGameOver();;
            }
        }
        else
        {
            var rand = UnityEngine.Random.Range(0, emptys.Count);
            var pt = emptys[rand].point;
            CreateBlock(pt.x, pt.y);
        }
    }
    private void Awake()
    {
        CreateBoard();  
    }

    public void UpdateState()
    {
        bool targetAllNull = true;
        foreach (var data in realNodeList)
        {
            if (data.target != null)
            { 
                targetAllNull = false;
                break;
            }
        }

        if (targetAllNull)
        {
            if (state == State.PROCESSING)
            { 
                var removed = new List<NodeObject>();
                List<NodeObject> removeTarget = new List<NodeObject>();
                foreach (var data in realNodeList) 
                    if (data.needDestroy)  
                        removeTarget.Add(data);
                
                removeTarget.ForEach(x =>
                {
                    realNodeList.Remove(x);
                    GameObject.Destroy(x.gameObject);
                });
                state = State.END;
            }
        }

        if (state == State.END)
        {
            nodeData.ForEach(x => x.combined = false);
//            Debug.Log("init nodes, try move!");
            state = State.WAIT;
            CreateRandom();
        }
    }

    private void Show()
    {
        string v = null;
        for (int i = col-1; i >= 0; i--)
        {
            for (int j = 0; j < row; j++)
            {
                var p = nodeMap[new Vector2Int(j, i)].value;
                string t = p.ToString();
                if (p.HasValue == false)
                {
                    t = "N";
                } 
                if (p == 0) t = "0";
                    
                v += t + " ";
            } 
            v += "\n";
        } 
        Debug.Log(v);
    }

    public void RestartGame()
    {
        // 1. 隐藏失败面板
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        isGameOver = false;

        // 2. 重置计分、计时数据
        score = 0;
        if (scoreText != null) scoreText.text = "Score: 0";
        elapsedTime = 0f;
        isTimerRunning = true;
        if (timerText != null) timerText.text = "00:00";

        // 3. 清理棋盘上的所有方块（销毁物体并清空数据）
        // 先销毁所有真实的方块 GameObject
        foreach (var obj in realNodeList)
        {
            if (obj != null) GameObject.Destroy(obj.gameObject);
        }
        realNodeList.Clear();

        // 4. 重置数据层（NodeData）的所有数值
        foreach (var node in nodeData)
        {
            node.value = null;
            node.realNodeObj = null;
            node.combined = false;
        }

        // 5. 重新生成初始方块（两个）
        for (int i = 0; i < 2; i++)
        {
            CreateRandom();
        }

        // 6. 把状态重置到 WAIT
        state = State.WAIT;
    }

    private void Update()
    {
        UpdateState();

        // --- 新增计时器更新逻辑 ---
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime; // 累加每帧的时间增量

            // 计算分钟和秒
            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);

            // 格式化为 00:00 并显示
            if (timerText != null)
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (state == State.WAIT)
        {
            if (Input.GetKeyUp(KeyCode.RightArrow)) MoveTo(Node.Direction.RIGHT);
            if (Input.GetKeyUp(KeyCode.LeftArrow)) MoveTo(Node.Direction.LEFT);
            if (Input.GetKeyUp(KeyCode.UpArrow)) MoveTo(Node.Direction.UP);
            if (Input.GetKeyUp(KeyCode.DownArrow)) MoveTo(Node.Direction.DOWN);
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            Show();
        }
    }

    private void Start()
    {
        // 读取本地存储的最高分，如果不存在则默认 0
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        if (bestScoreText != null) bestScoreText.text = "Best: " + bestScore;

        elapsedTime = 0f;
        isTimerRunning = true; // 游戏开始时启动计时
        if (timerText != null) timerText.text = "00:00"; // 初始显示


        for (int i = 0; i < 2; i++)
        {
            CreateRandom();
        }
    }
}
 

 
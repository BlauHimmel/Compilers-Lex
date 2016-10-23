using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lex
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                FiniteMachine fm = new FiniteMachine();
                Console.WriteLine("输入正则表达式(首尾要打括号，注意不要输入中文标点符号)：");
                String regex = Console.ReadLine();
                if (fm.CreateNFA(regex))
                {
                    fm.DeleteNullEdge();
                    Console.WriteLine("DFA表：\n");
                    fm.PrintTable(fm.GetDFATable());
                }
            }
        }
    }

    class Node
    {
        public String name;
        //指针集合，每一个指针上附带一个Key
        public Dictionary<String, Node> array;
        //是否是最终节点
        public bool isEnd;
        private int nullNumber;

        public Node(String name)
        {
            array = new Dictionary<String,Node>();
            this.name = name;
            this.nullNumber = 0;
            this.isEnd = false;
        }

        public Node Copy()
        {
            Node node = new Node(this.name);
            node.isEnd = this.isEnd;
            return node;
        }

        public void Increase()
        {
            nullNumber++;
        }

        public void Decrease()
        {
            nullNumber--;
        }

        public int GetNullNumber()
        {
            return nullNumber;
        }

        public void Destroy()
        {
            name = null;
            array.Clear();
            nullNumber = 0;
            this.isEnd = false;
        }
    }

    class StartEndInfo
    {
        public Node start;
        public Node end;

        public StartEndInfo(Node start, Node end)
        {
            this.start = start;
            this.end = end;
        }
    }

    class FiniteMachine
    {
        private Node root = null;
        private Node end = null;
        //所有由非null状态转化而来节点的集合(包含起始节点)
        private HashSet<Node> validSet = null;
        //所有由null状态转化而来节点的集合(不包含起始节点)
        private HashSet<Node> invalidSet = null;

        public FiniteMachine()
        {
            validSet = new HashSet<Node>();
            invalidSet = new HashSet<Node>();
        }

        /// <summary>
        /// 根据正则表达式构建一个NFA
        /// </summary>
        /// <param name="regex"></param>
        public bool CreateNFA(String regex)
        {
            Stack<char> operatorStack = new Stack<char>();
            Stack<StartEndInfo> seStack = new Stack<StartEndInfo>();
            int count = 1;
            for (int i = 0; i < regex.Length; i++)
            {
                char a = regex.ElementAt(i);

                if (a == '(' || a == '|')
                {
                    operatorStack.Push(a);
                    continue;
                }

                if (a == ')')
                {
                    char topOperator = operatorStack.Pop();
                    while (topOperator != '(')
                    {
                        if (topOperator == '|')
                        {
                            StartEndInfo info1 = seStack.Pop();
                            StartEndInfo info2 = seStack.Pop();
                            Node start = new Node("start");
                            Node end = new Node("end");
                            invalidSet.Add(start);
                            invalidSet.Add(end);
                            Link(start, info1.start, "null");
                            Link(start, info2.start, "null");
                            Link(info1.end, end, "null");
                            Link(info2.end, end, "null");
                            StartEndInfo newInfo = new StartEndInfo(start, end);
                            seStack.Push(newInfo);
                            topOperator = operatorStack.Pop();
                        }
                        else
                        {
                            Console.WriteLine("表达式可能出现非法的运算符！");
                            return false;
                        }
                    }
                    if (topOperator != '(')
                    {
                        Console.WriteLine("括号不匹配！");
                        return false;
                    }
                    continue;
                }

                if (a == '*')
                {
                    StartEndInfo info = seStack.Pop();
                    Node start = new Node("start");
                    Node end = new Node("end");
                    invalidSet.Add(start);
                    invalidSet.Add(end);
                    Link(start, end, "null");
                    Link(start, info.start, "null");
                    Link(info.end, end, "null");
                    Link(info.end, start, "null");
                    StartEndInfo newInfo = new StartEndInfo(start, end);
                    seStack.Push(newInfo);
                    continue;
                }

                if (a == '+')
                {
                    StartEndInfo info = seStack.Pop();
                    Node start = new Node("start");
                    Node end = new Node("end");
                    invalidSet.Add(start);
                    invalidSet.Add(end);
                    Link(start, info.start, "null");
                    Link(info.end, end, "null");
                    Link(end, start, "null");
                    StartEndInfo newInfo = new StartEndInfo(start, end);
                    seStack.Push(newInfo);
                    continue;
                }

                if (!isOperator(a))
                {
                    Node start = new Node("start");
                    Node node = new Node(a.ToString() + count.ToString("D2"));
                    count++;  
         
                    Node[] nodes = validSet.ToArray();
                    for (int p = 0; p < nodes.Length;p++ )
                    {
                        nodes[p].isEnd = false;
                    }
                    node.isEnd = true;
                    end = node;

                    invalidSet.Add(start);
                    Link(start, node, a.ToString());
                    validSet.Add(node);

                    int k = i + 1;
                    while (k < regex.Length && !isOperator(regex.ElementAt(k)))
                    {
                        k++;
                    }
                    k--;

                    if (k > i)
                    {
                        Node oldNode = node;
                        Node newNode;
                        for (int j = i + 1; j <= k; j++)
                        {
                            newNode = new Node(regex.ElementAt(j).ToString() + count.ToString("D2"));
                            count++;
                            Link(oldNode, newNode, regex.ElementAt(j).ToString());
                            oldNode = newNode;
                            validSet.Add(oldNode);
                        }
                        node = oldNode;

                        nodes = validSet.ToArray();
                        for (int p = 0; p < nodes.Length; p++)
                        {
                            nodes[p].isEnd = false;
                        }
                        node.isEnd = true;
                        end = node;
                    }
                    StartEndInfo info = new StartEndInfo(start, node);
                    seStack.Push(info);
                    i = k;
                    continue;
                }
            }

            if (operatorStack.Count != 0)
            {
                Console.WriteLine("错误，表达式读取完毕时，操作符栈中还存在有元素！");
                return false;
            }

            if (seStack.Count > 0)
            {
                StartEndInfo[] infos = seStack.ToArray();
                this.root = infos[infos.Length - 1].start;
                validSet.Add(this.root);
                invalidSet.Remove(this.root);
                if (infos.Length > 1)
                {
                    for (int i = infos.Length - 2; i >= 0; i--)
                    {
                        Link(infos[i + 1].end, infos[i].start, "null");
                    }                     
                }
            }
            return true;
        }

        /// <summary>
        /// 去除DFA中的空边
        /// </summary>
        public void DeleteNullEdge()
        {
            HashSet<Node> tmpSet = new HashSet<Node>();    
            //对所有的有效状态节点（通过一个非null的状态输入转换而来）、初始节点进行检索
            foreach (Node n in validSet)
            {
                tmpSet.Clear();
                //获得从当前节点出发，仅通过null边就可以到达的所有状态的集合，存入tmpSet集合中
                GetNullEdgeSet(tmpSet, n);
                //检索集合中的每一个节点中的每一个连接状态，如果有非空连接则将这个状态节点复制到节点n上，即n节点等效与tmpSet中的每一个节点
                foreach (Node nn in tmpSet)
                {
                    foreach (String key in nn.array.Keys)
                    {
                        if (!(key.Length >= 4 && key.Substring(0, 4).Equals("null")))
                        {
                            Link(n, nn.array[key], key);
                        }
                    }
                }
            }
        
            //删除所有的null边
            foreach(Node n in invalidSet)
            {
                n.Destroy();
            }
            //删除多余的指针
            foreach (Node n in validSet)
            {
                String[] keys = n.array.Keys.ToArray();
                foreach (String key in keys)
                {
                    Node nn = n.array[key];
                    if (nn.name == null && nn.array.Count == 0 && nn.GetNullNumber() == 0 && nn.isEnd == false)
                    {
                        n.array.Remove(key);
                    }
                    //当多余的指针指向开头时
                    if(nn == this.root)
                    {
                        n.array.Remove(key);
                    }
                }
            }
        }

        /// <summary>
        /// 构建一个DFA表，表结构为：
        /// 字典（节点名，字典（通过状态转换字符，到达节点名称的集合））
        /// 可以使用PrintTable在控制台上输出这张表
        /// </summary>
        /// <returns></returns>
        public Dictionary<String, Dictionary<String, HashSet<String>>> GetDFATable()
        {  
            //首先构造表
            //K值为节点名称,V为通过字典，V中的K表示通过的状态转换字符，V表示所到达节点的集合
            //字典<节点名，字典<通过状态转换字符，到达节点名称的集合>>
            Dictionary<String, Dictionary<String, HashSet<String>>> table = new Dictionary<String, Dictionary<String, HashSet<String>>>(); 
            foreach (Node n in validSet)
            {
                //当前节点名
                String name = n.name;
                //字典<通过状态转换字符，到达节点名称的集合>
                Dictionary<String, HashSet<String>> tmpDict = new Dictionary<String,HashSet<String>>();
                //到达节点名称的集合
                HashSet<String> tmpSet;
                foreach (String key in n.array.Keys)
                {
                    //对应通过状态转换字符如果集合对象没有创建则创建集合对象并将当前节点的名称加入到集合中
                    if(tmpDict.TryGetValue(key.Substring(0,1), out tmpSet))
                    {
                        tmpSet.Add(n.array[key].name);
                    }
                    else
                    {
                        tmpSet = new HashSet<String>();
                        tmpSet.Add(n.array[key].name);
                        tmpDict.Add(key.Substring(0,1),tmpSet);
                    }
                }
                table.Add(name, tmpDict); 
            }
            return table;
        }

        /// <summary>
        /// 构建一个NFA表，表结构为：
        /// 字典（节点名，字典（通过状态转换字符，到达节点名称的集合））
        /// 可以使用PrintTable在控制台上输出这张表 
        /// </summary>
        /// <returns></returns>
        public Dictionary<String, Dictionary<String, HashSet<String>>> GetNFATable()
        {
            Queue<String> queue = new Queue<String>();
            //记录DFA中新节点的集合
            HashSet<String> set = new HashSet<String>();

            Dictionary<String, Dictionary<String, HashSet<String>>> tableDFA = GetDFATable();
            Dictionary<String, Dictionary<String, HashSet<String>>> tableNFA = new Dictionary<String, Dictionary<String, HashSet<String>>>();

            //临时字典，用于插入到tableDFA中
            Dictionary<String, HashSet<String>> tmpDict;
            //临时字典，用于记录多个节点在同一个Key下转换状态节点的名称集合
            Dictionary<String, HashSet<String>> tmpDict2;
            //临时集合，用于tableDFA的D子字典中
            HashSet<String> tmpSet;
            //临时集合，用于插入到tmpDict2中
            HashSet<String> tmpSet2;

            String start = this.root.name;

            queue.Enqueue(start);
            while(queue.Count() > 0)
            {
                tmpDict = new Dictionary<String, HashSet<String>>();
                tmpDict2 = new Dictionary<String, HashSet<String>>();

                String node = queue.Dequeue();
                tableNFA.Add(node, tmpDict);

                for (int i = 0; i < node.Length; i += 3) 
                {
                    String nodeName;
                    if (node.Equals("start"))
                    {
                        nodeName = node;
                        i = int.MaxValue / 2;
                    }
                    else
                    {
                        nodeName = node.Substring(i, 3);
                    }
                    
                    //当前节点的转换状态的字符的并集
                    foreach (String key in tableDFA[nodeName].Keys)
                    {
                        //新节点,nodeName节点通过状态转换Key可以到达的节点之和（名称相加）
                        String newNode = "";
                        //存在一个转换状态指向多个节点
                        if (tableNFA[nodeName][key].Count >= 1)
                        {
                            foreach (String n in tableNFA[nodeName][key])
                            {
                                newNode = newNode + n;
                            }
                        }
                        //记录通过key状态转换到达的节点的集合
                        if (tmpDict2.TryGetValue(key, out tmpSet2))
                        {
                            tmpSet2.Add(newNode);
                        }
                        else
                        {
                            tmpSet2 = new HashSet<String>();
                            tmpSet2.Add(newNode);
                            tmpDict2.Add(key, tmpSet2);
                        }
                    }
                }

                foreach (String key in tmpDict2.Keys)
                {
                    String newNodeName = "";
                    //生成组合成新节点的名字
                    HashSet<String> tmp = new HashSet<String>();
                    foreach(String n in tmpDict2[key])
                    {
                        if (!n.Equals("start"))
                        {
                            for (int u = 0; u < n.Length; u += 3)
                            {
                                tmp.Add(n.Substring(u, 3));
                            } 
                        }        
                    }
                    foreach (String n in tmp)
                    {
                        newNodeName = newNodeName + n;
                    }
                    //如果在状态图中已经存在这个节点则不加入队列和结果表中
                    if (!set.Contains(newNodeName))
                    {
                        set.Add(newNodeName);
                        queue.Enqueue(newNodeName);
                    }
                    //加入结果表
                    tmpSet = new HashSet<String>();
                    tmpSet.Add(newNodeName);
                    tmpDict.Add(key, tmpSet);
                }        
            }
            return tableNFA;
        }

        /// <summary>
        /// 在控制台中输出表
        /// </summary>
        /// <param name="table"></param>
        public void PrintTable(Dictionary<String, Dictionary<String, HashSet<String>>> table)
        {
            foreach(String node in table.Keys)
            {
                Console.Write(node + " : ");
                Dictionary<String, HashSet<String>> tmpDict = table[node];
                foreach(String state in tmpDict.Keys)
                {
                    Console.Write(state + "->");
                    HashSet<String> tmpSet = tmpDict[state];
                    foreach(String target in tmpSet)
                    {
                        Console.Write(target + ",");
                    }
                }
                if (end.name.Equals(node))
                {
                    Console.Write("end");
                }
                Console.WriteLine("");
            }
        }

        /// <summary>
        /// 返回当前DFA起始节点的引用
        /// </summary>
        /// <returns></returns>
        public Node GetRoot()
        {
            return this.root;
        }

        /// <summary>
        /// 将与start节点相连的边为null*的节点加入到集合sets中
        /// </summary>
        /// <param name="sets">存储用的集合</param>
        /// <param name="start">开始的节点</param>
        private void GetNullEdgeSet(HashSet<Node> sets, Node start)
        {
            foreach (String n in start.array.Keys)
            {
                if (n.Length >= 4 && n.Substring(0, 4).Equals("null")) 
                {
                    sets.Add(start.array[n]);
                    GetNullEdgeSet(sets, start.array[n]);
                }
            }
        }

        /// <summary>
        /// 把node1指向node2，状态名称为name,如果没有状态则name的值为"null"
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="name"></param>
        private void Link(Node node1, Node node2, String name)
        {
            if (name.Length >= 4 && name.Substring(0, 4).Equals("null"))
            {
                node1.Increase();
                node1.array.Add(name + "-" + node1.GetNullNumber(), node2);
            }
            else if (name.Length == 1 && !isOperator(name.ElementAt(0)))
            {
                node1.Increase();
                node1.array.Add(name.Substring(0, 1) + "-" + node1.GetNullNumber(), node2);
            }
            else if(name.Length >= 2 && !isOperator(name.ElementAt(0)) && name.ElementAt(1) == '-')
            {
                node1.Increase();
                node1.array.Add(name.Substring(0, 1) + "-" + node1.GetNullNumber(), node2); 
            }
            else
            {
                node1.array.Add(name, node2); 
            }
        }

        /// <summary>
        /// 判断当前字符是否时操作符
        /// </summary>
        /// <param name="a"></param>
        /// <returns>如果时则返回true，否则返回false</returns>
        private bool isOperator(char a)
        {
            if (a == '(' || a == ')' || a == '+' || a == '*' || a == '|') 
            {
                return true;
            }
            return false;
        }
    }
}

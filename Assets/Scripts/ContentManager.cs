using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;

/// <summary>
/// 问答组合
/// </summary>
public class Content {
    // 问题以及答案
    public Texture2D question, answer;
    // 问题图片的绝对路径
    public string fullName;
    // 问题图片的相对路径
    public string RelativePath
    {
        get { return fullName.Substring(ContentManager.contentRootPath.Length); } // 去掉前面的绝对路径，只要文件夹+图片的部分
    }
    // 构造函数
    public Content(Texture2D ques,Texture2D ans, string fullName)
    {
        question = ques;
        answer = ans;
        this.fullName = fullName;
    }
}

[Serializable]
public class MarkedQandA
{
    // 记录所有已标记的相对地址
    public string[] allContents;

    public MarkedQandA(string[] value)
    {
        allContents = value;
    }
}

public static class JsonData
{
    public static string markJsonPath = ""; // Start里面定义
    public static MarkedQandA markedQand;

    /// <summary>
    /// 保存已标记问题的列表
    /// </summary>
    /// <param name="value"></param>
    public static void SaveMarkList(List<string> value)
    {
        // 转成可序列化的内容
        markedQand = new MarkedQandA(value.ToArray());
        
        // 转成Json
        string json = JsonUtility.ToJson(markedQand);

        // 写成Json
        File.WriteAllText(markJsonPath, json);
    }
    
    public static List<String> ReadMarkList()
    {
        List<string> res = new List<string>();

        // 如果没有该文件则返回
        if (!File.Exists(markJsonPath)) return res;

        // 读取Json
        StreamReader streamReader = new StreamReader(markJsonPath);
        string jsonStr = streamReader.ReadToEnd();
        markedQand = JsonUtility.FromJson<MarkedQandA>(jsonStr);

        // 转成List
        for (int i = 0; i < markedQand.allContents.Length; i++)
        {
            res.Add(markedQand.allContents[i]);
        }

        return res;
    }
}

public class ContentManager : MonoBehaviour {
    #region UI
    // log
    public Text logText;
    // 显示的图像
    public RawImage image;
    // 排序按钮
    public Toggle sortToggle;
    // 加载进度条
    public Slider loadingSlider;
    // 选择文件夹
    public Dropdown folderDropdown;
    // 标记按钮
    public Toggle markToggle;
    // 标记过滤按钮
    public Toggle markedFitlerToggle;
    #endregion

    #region 容器
    // 加载的所有问答组合
    public List<Content> contents = new List<Content>();
    // 所有文件夹
    public List<string> allFolders = new List<string>();
    // 存放加载的贴图
    Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
    // 存放序号的数组
    private int[] currentIndexesArr = null;
    // 标记的内容
    private List<string> markedList = new List<string>();
    #endregion
    // 存放所有内容的根路径
    public static string contentRootPath;
    // 当前的序号
    private int currentIndex = 0;
    // 当前的排序类型
    public enum SortType
    {
        random,
        order
    }
    public SortType sortType;

    private void Start()
    {
        // 初始化内容的根目录
        contentRootPath = Application.dataPath + "/ContentData/";
        // 初始化标记json文件的路径
        JsonData.markJsonPath = Application.dataPath + "/marked.json";
        // 读取json
        markedList = JsonData.ReadMarkList();
        // 加载文件夹
        LoadFolders();
        // 读取现在是乱序还是顺序
        sortType = sortToggle.isOn ? SortType.random : SortType.order;
        // 加载当前选择的文件的内容
        LoadContentInFolder(allFolders[folderDropdown.value]);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Next"))
        {
            NextQuestion();
        }else if (Input.GetButtonDown("Last"))
        {
            LastQuestion();
        }else if (Input.GetButtonDown("ShowAnswer"))
        {
            ShowAnswer();
        }
    }

    /// <summary>
    /// 加载一个文件夹内的问答内容
    /// </summary>
    public void LoadContentInFolder(string folder)
    {
        // 清空
        loadedTextures.Clear();
        contents.Clear();

        // 打开进度条
        loadingSlider.gameObject.SetActive(true);
        Log("加载 " + folder + " 中...");

        // 读取一个文件夹
        string currentPath = contentRootPath + folder + "/";
        if (Directory.Exists(currentPath))
        {
            DirectoryInfo direction = new DirectoryInfo(currentPath);
            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
            StartCoroutine(LoadImage(files));
        }
        else
        {
            Log(currentPath + ",此路径不存在!");
        }
    }
    /// <summary>
    /// 加载所有内容
    /// </summary>
    public void LoadAllContent()
    {
        // 清空
        loadedTextures.Clear();
        contents.Clear();

        // 读取所有文件夹
        List<FileInfo> filesList = new List<FileInfo>();
        // 合并文件夹内容
        foreach (string item in allFolders)
        {
            // 打开进度条
            loadingSlider.gameObject.SetActive(true);
            Log("加载 全部 中...");

            string currentPath = contentRootPath + item + "/";
            if (Directory.Exists(currentPath))
            {
                DirectoryInfo direction = new DirectoryInfo(currentPath);
                filesList.AddRange(new List<FileInfo> (direction.GetFiles("*", SearchOption.AllDirectories)));
            }
            else
            {
                Log(currentPath + ",此路径不存在!");
            }
        }
        // 开始加载
        StartCoroutine(LoadImage(filesList.ToArray()));
    }

    /// <summary>
    /// 读取所有分类文件夹
    /// </summary>
    public void LoadFolders()
    {
        // 清空
        allFolders.Clear();
        folderDropdown.options.Clear();
        DirectoryInfo direction = new DirectoryInfo(contentRootPath);
        DirectoryInfo[] subDirs = direction.GetDirectories();

        // 记录每一个文件夹的名字
        foreach (DirectoryInfo item in subDirs)
        {
            allFolders.Add(item.Name);
        }

        // 设置下拉选择栏
        folderDropdown.AddOptions(allFolders);
        folderDropdown.AddOptions(new List<string>{ "全部" });
    }

    /// <summary>
    /// 选择文件夹
    /// </summary>
    /// <param name="index"></param>
    public void SelectFolder(int index)
    {
        // 存一下当前json
        JsonData.SaveMarkList(markedList);

        if (index == allFolders.Count) //全部
        {
            LoadAllContent();
        }
        else
        {
            LoadContentInFolder(allFolders[index]); // 特定文件夹
        }
    }

    /// <summary>
    /// 加载图片
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadImage(FileInfo[] files)
    {
        for (int i = 0; i < files.Length; i++)
        {
            loadingSlider.value = (float)i / (files.Length - 1); // 进度条
            if (files[i].Name.EndsWith(".meta")) continue;

            // 加载图片
            WWW www = new WWW(files[i].FullName);
            if (www != null && string.IsNullOrEmpty(www.error))
            {
                loadedTextures[files[i].FullName.Remove(files[i].FullName.LastIndexOf('.'))] = www.texture;
            }
            yield return null;
        }
        AnalyzeImage();
    }
    /// <summary>
    /// 分析图片
    /// </summary>
    private void AnalyzeImage()
    {
        // 关闭进度条
        loadingSlider.gameObject.SetActive(false);
        Texture2D tempT2d;
        int numOfQanA = 0;
        foreach (KeyValuePair<string, Texture2D> item in loadedTextures)
        {
            // 是答案就跳过
            if (item.Key.EndsWith("-")) continue;

            // 如果存在对应的答案
            if (loadedTextures.TryGetValue(item.Key + "-", out tempT2d))
            {
                // 加入问答组合中
                contents.Add(new Content(item.Value, tempT2d, item.Key));
                numOfQanA++;
            }
            else
            {
                Log(item.Key + " 没有对应的答案");
            }
        }
        Log(string.Format("成功加载{0}个问答组合", numOfQanA));

        // 处理排序
        ReSortIndexArr();

        // 自动切换问题
        currentIndex = 0;
        NextQuestion();
    }

    /// <summary>
    /// 设置是否标记
    /// </summary>
    /// <param name="value"></param>
    public void SetMark(bool value)
    {
        // 获取当前图片问题的相对路径
        string currentName = contents[currentIndexesArr[currentIndex]].RelativePath;

        if (value) // 标记
        {
            if (!markedList.Contains(currentName)) // 不存在才添加（因为用代码修改Toggle状态也会调用这个函数）
                markedList.Add(currentName);
        }
        else // 取消标记
        {
            if (markedList.Contains(currentName))
                markedList.Remove(currentName);
            else
                Debug.Log(currentName + " 取消标记时不存在与List中。");
        }
    }

    /// <summary>
    /// 设置随机排序
    /// </summary>
    /// <param name="value"></param>
    public void SetRandom(bool value)
    {
        if (value) sortType = SortType.random;
        else sortType = SortType.order;

        // 处理排序
        ReSortIndexArr();
        currentIndex = 0;
    }
    /// <summary>
    /// 根据随机状态设置索引数组
    /// </summary>
    public void ReSortIndexArr()
    {
        // 初始化为顺序排序的索引
        currentIndexesArr = new int[contents.Count];
        for (int i = 0; i < contents.Count; i++)
        {
            currentIndexesArr[i] = i;
        }

        // 如果是随机排序就随机排序
        if (sortType == SortType.random)
        {
            Dictionary<int, int> tempDic = new Dictionary<int, int>();
            int tempNum = 0;
            for (int i = 0; i < contents.Count; i++)
            {
                do
                { tempNum = UnityEngine.Random.Range(0, contents.Count); }
                while (tempDic.ContainsValue(tempNum));
                tempDic[i] = tempNum;
            }
            for (int i = 0; i < contents.Count; i++)
            {
                currentIndexesArr[i] = tempDic[i];
            }
        }
    }

    /// <summary>
    /// 显示答案
    /// </summary>
    public void ShowAnswer()
    {
        if (contents.Count == 0) return;

        ChangeImage(contents[currentIndexesArr[currentIndex]].answer);

        CheckMarked();
    }
    /// <summary>
    /// 下一问题
    /// </summary>
    public void NextQuestion()
    {
        if (contents.Count == 0) return;
        if (markedFitlerToggle.isOn) // 开启过滤的话
        {
            int tempIndex = currentIndex; // 记录一下当前的index
            do
            {
                ++currentIndex;
                if (currentIndex >= currentIndexesArr.Length) currentIndex = 0;
                if (tempIndex == currentIndex) // 如果饶了一周还是原来这个就跳出
                {
                    Log("没有了");
                    break;
                }
            } while (!markedList.Contains(contents[currentIndexesArr[currentIndex]].RelativePath));// 找到下一个标记的为止
        }
        else // 没有开启过滤的话
        {
            ++currentIndex;
            if (currentIndex >= currentIndexesArr.Length) currentIndex = 0;
        }
            
        ChangeImage(contents[currentIndexesArr[currentIndex]].question);

        CheckMarked();
    }
    /// <summary>
    /// 上一个问题
    /// </summary>
    public void LastQuestion()
    {
        if (contents.Count == 0) return;

        if (markedFitlerToggle.isOn) // 开启过滤的话
        {
            int tempIndex = currentIndex; // 记录一下当前的index
            do
            {
                --currentIndex;
                if (currentIndex < 0) currentIndex = currentIndexesArr.Length - 1;
                if(tempIndex == currentIndex) // 如果饶了一周还是原来这个就跳出
                {
                    Log("没有了");
                    break;
                }
            } while (!markedList.Contains(contents[currentIndexesArr[currentIndex]].RelativePath));// 找到下一个标记的为止
        }
        else // 没有开启过滤的话
        {    
            --currentIndex;
            if (currentIndex < 0) currentIndex = currentIndexesArr.Length - 1;
        }
        // 修改图片
        ChangeImage(contents[currentIndexesArr[currentIndex]].question);

        CheckMarked();
    }

    /// <summary>
    /// 更换图片
    /// </summary>
    /// <param name="texture"></param>
    private void ChangeImage(Texture2D texture)
    {
        image.texture = texture;
        image.SetNativeSize();
    }

    /// <summary>
    /// 检查当前问题有没有被标记
    /// </summary>
    private void CheckMarked()
    {
        // 查找到已标记就打开Toggle，没有就关闭
        markToggle.isOn = markedList.Contains(contents[currentIndexesArr[currentIndex]].RelativePath);
    }

    /// <summary>
    /// 退出
    /// </summary>
    public void Quit()
    {
        // 存一下当前json
        JsonData.SaveMarkList(markedList);

#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Log
    /// </summary>
    /// <param name="content"></param>
    public void Log(string content)
    {
//#if UNITY_EDITOR
//        Debug.Log(content);
//#else
        logText.text = content;
//#endif
    }
}
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 问答组合
/// </summary>
public class Content {
    // 问题以及答案
    public Texture2D question, answer;
    // 构造函数
    public Content(Texture2D ques,Texture2D ans)
    {
        question = ques;
        answer = ans;
    }
}

public class ContentManager : MonoBehaviour {
    // logUI
    public Text logText;
    // 显示的图像
    public RawImage image;
    // 排序按钮
    public Toggle sortToggle;
    // 加载进度条
    public Slider loadingSlider;
    // 选择文件夹
    public Dropdown folderDropdown;
    // 存放所有内容的根路径
    private static string mainPath;
    // 加载的所有问答组合
    public List<Content> contents = new List<Content>();
    // 所有文件夹
    public List<string> allFolders = new List<string>();
    // 存放加载的贴图
    Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
    // 存放序号的数组
    private int[] currentIndexesArr = null;
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
        mainPath = Application.dataPath + "/ContentData/";
        LoadFolders();
        sortType = sortToggle.isOn ? SortType.random : SortType.order;
        LoadContentInFolder(allFolders[folderDropdown.value]);
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
        string currentPath = mainPath + folder + "/";
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

            string currentPath = mainPath + item + "/";
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
        DirectoryInfo direction = new DirectoryInfo(mainPath);
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
        if(index == allFolders.Count) //全部
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
                contents.Add(new Content(item.Value, tempT2d));

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
                { tempNum = Random.Range(0, contents.Count); }
                while (tempDic.ContainsValue(tempNum));
                tempDic[i] = tempNum;
            }
            for (int i = 0; i < contents.Count; i++)
            {
                currentIndexesArr[i] = tempDic[i];
            }
        }

        for (int i = 0; i < contents.Count; i++)
        {
            Debug.Log(currentIndexesArr[i]);
        }
    }

    /// <summary>
    /// 显示答案
    /// </summary>
    public void ShowAnser()
    {
        if (contents.Count == 0) return;

        ChangeImage(contents[currentIndex].answer);
    }
    /// <summary>
    /// 下一问题
    /// </summary>
    public void NextQuestion()
    {
        if (contents.Count == 0) return;

        ++currentIndex;
        if (currentIndex >= currentIndexesArr.Length) currentIndex = 0;
        int temp = currentIndex;

        ChangeImage(contents[currentIndexesArr[temp]].question);
    }
    /// <summary>
    /// 上一个问题
    /// </summary>
    public void LastQuestion()
    {
        if (contents.Count == 0) return;
        --currentIndex;
        if (currentIndex < 0) currentIndex = currentIndexesArr.Length - 1;
        int temp = currentIndex;

        ChangeImage(contents[currentIndexesArr[temp]].question);
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
    /// 退出
    /// </summary>
    public void Quit()
    {
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
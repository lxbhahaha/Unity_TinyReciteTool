# Unity_TinyReciteTool
 方便自己背书，用Unity做的一个简单的随机背书的小工具（比较简陋，自己凑合着用），读取文件夹中的图片作为内容，点击按钮之后再显示答案。

在左上角的下拉菜单中有选项，可选择不同的类别或者全部。下方开关可以切换乱序还是顺序背诵。
![image](https://raw.githubusercontent.com/lxbhahaha/Unity_TinyReciteTool/master/Pictures/%E7%95%8C%E9%9D%A2.png)

## 添加数据

在电脑上自己喜欢的位置创建一个文件夹“newFile”，（newFile叫什么都行，随意）。然后在“newFile”文件夹中创建一个`ContentData`，`ContentData`文件夹里面按子文件夹分类，子文件夹中放入图片，图片名称后加“`-`”表示是答案。

在文件夹 `随机背书_Data\`中创建一个`path.txt`(有则不用创建)，里面放上“newFile”的绝对路径。==注意，分隔符要用反斜杠。两个斜杠也不行==。

如果是在Unity源工程中在 `Assets`下创建`path.txt`。
![image](https://github.com/lxbhahaha/Unity_TinyReciteTool/blob/master/Pictures/ContentData.png?raw=true)


# VersionUpdateBundled

这是一个集合Unity的[AssetBundlesBrowser](https://github.com/Unity-Technologies/AssetBundles-Browser)和[AssetBundleDemo](https://bitbucket.org/Unity-Technologies/assetbundledemo)，只是对AssetBundleManager做了太多修改，并且也对AssetBundlesBrowser做了代码侵入修改，再加入了版本更新功能。

## 特性
1. 使用Unity [AssetBundlesBrowser](https://github.com/Unity-Technologies/AssetBundles-Browser)来打Bundle，这个工具的好处就不用我再多说了，有兴趣可以超链接过去看看。
2. 资源加载透明。使用Bundle之后，加载文件方式就变成了需要程序先加载Bundle，使用Bundle再加载文件，跟Resources.Load接口的文件路径不一样，然后再跟AssetDatabase.LoadAssetAtPath接口的文件路径又不一样，这简直就是搞事情，增加使用者的劳动成本。所以VersionUpdateBundle使用统一接口来加载Bundle，不管是在PC上还是手机平台上，都是一样的接口，都是一样的文件路径。文件路径跟AssetDatabase.LoadAssetAtPath接口一样，默认从Assets目录开始，包含Assets目录。加载示例：
    ```csharp
    AssetBundleManager.Instance.LoadSceneAsync("Assets/VersionUpdateBundle/Examples/BundleAssets/Tanks/Scenes/TanksExample.unity", LoadSceneMode.Single)
    AssetBundleManager.Instance.LoadAssetAsync("Assets/VersionUpdateBundle/Examples/Background/Background.prefab")
    AssetBundleManager.Instance.LoadAsset<GameObject>("Assets/VersionUpdateBundle/Examples/Background/Background.prefab")
    ```
3. 模拟模式。这是AssetBundleManager一个非常好用的功能，用来加载Bundle和本地文件时，都是一样的方法，这么好的功能，当然一并继承下来。模拟模式就是方便开发人员，在开发过程中，不必每添加一个资源，就打一次Bundle，然后才能使用Bundle把它Load出来。而是只要放到Unity的工程Assets目录下，使用文件目录进行加载，开启模拟模式，就可以在PC不用打Bundle包，就Load到了资源，其结果跟打完Bundle效果完全一样，非常方便开发和测试。
4. 打Bundle跟程序开发完全分离。有了资源加载透明功能，有了模拟模式开发功能，开发人员完全可以对于Bundle是怎么打的，Bundle的资源都是怎么分的，Bundle的资源都是一日改了几次的所有这些问题，没有一点关系，打Bundle的事，完全可以在开发的任何一个阶段，由任意一个开发人员使用AssetBundlesBrowser去操作，而不用担心会使代码的资源加载出现问题。夸张一点说，你的Bundle是爱打成一个，还是打成N个，跟程序开发没有关系，程序开发只需要按资源在项目的Assets目录下加载即可。
5. 版本更新。说到版本更新，其实就是增量更新，发布完应用后，需要新增、删除、修改原来的资源。这个时候就是版本更新要干的事了。VersionUpdateBundled支持增量更新，支持多个版本一起更新，支持自动打出增量包。（有兴趣请看下方的原理介绍。）

## 快速上手
1. 打开Assets/VersionUpdateBundle/Examples/Scenes/Scens.unity场景
2. 开启模拟模式。在菜单栏上选择AssetBundles/Simulation Mode
3. 点击Play运行Unity，就可以看到这个示例了。示例内容
    > 在进入游戏时，快速显示一个背景，而不是长时间黑屏无响应的资源加载。然后就是资源更新检查的进度条显示，最后才是加载和切换场景。

## 打bundle的操作流程
1. 在菜单栏上选择AssetBundles/VersionUpdatePackage，会打开以下这个窗口。说一下各项的意思

    ![Alt text](/ScreenShot/VersionUpdatePackage.jpg)
    - Version Update Folder 这是在初次对一个平台打Bundle时需要指定的路径，然后点击Create Version Update Folder按钮，将会创建该平台的Bundle打包和发布路径。示例图中就是打包PC版本Bundle的路径。点击创建按钮以后，会在文件夹下生成三个文件夹，分别是AssetBundles、LastManifest、Publish，不同的平台需要创建不同的路径。
    - Version Update Path 版本更新使用的文件夹，也就是上面所创建的文件夹。
    - Last Manifest Path 就是上面创建的文件夹，用来打增量版本时，存放旧的Manifest文件
    - Asset Bundles Path 上面创建的文件夹，用来存放项目打包的AssetBundle，如果是手机平台，这些资源需要再拷贝到项目Assets/StreamingAssets下面，这项操作在AssetBundlesBrowser Build的时候，会有一个Copy to StreamingAssets的选项。
    - Publish Path 上面创建的文件夹，用来存放项目打发布Bundle包时，需要放到服务器提供给客户端更新的资源，资源会被打成zip，还有一个VersionUpdateTable.json文件。
    - Last Version Number 打更新包时，读取LastManifestPath下旧资源的版本号
    - New Version Number 打更新包时，读取AssetBundlesPath下新资源的版本号
    - Copy To LastManifest 打更新包时，自动把旧资源的Mainifest和版本文件拷贝到LastManifest文件夹下
    - Create Version Package 把所有Bundle都打成更新包，在PublishPath下生成一个以版本号命名的zip文件，并创建或者更新PublishPath下的VersionUpdateTable.json文件。
    - Generate Incremental Package 打增量更新包，读取LastManifestPath下的旧版本Manifest文件，再读取AssetBundlesPath新的Bundle文件，进行Bundle对比，把新增的，改动的Bundle打成一个更新包放在PublishPath下，以版本号命名的zip文件，并创建或者更新PublishPath下的VersionUpdateTable.json文件。
    - 以下是各文件生成后存放资源的示意图：
            
    ![Alt text](/ScreenShot/VersionUpdateFolder.png)
    ![Alt text](/ScreenShot/AssetBundlesFolder.png)
    ![Alt text](/ScreenShot/LastManifestFolder.png)
    ![Alt text](/ScreenShot/PublishFolder.png)

2. 创建完Bundle路径后，就可以打开AssetBundlesBrowser，开始打Bundle了。在菜单栏上选择Windows/AssetBundle Browser，会打开以下这个窗口。这是Unity的官方工具，具体使用情况，请看官方说明。这是传送门[https://github.com/Unity-Technologies/AssetBundles-Browser](https://github.com/Unity-Technologies/AssetBundles-Browser)

![Alt text](/ScreenShot/AssetBundlesBrowser.png)

> * 要记得Build Target要跟你创建版本更新的名字一样，不同平台，对应不同的版本更新目录，Output Path也是对应到版本更新创建的AssetBundles目录
> * 建议勾上Clear Folders。如果是要发布一个完整手机安装包的，记得勾Copy to StreamingAssets，这样打出来的Bundle才和安装包一起。

## 部分代码的原理与配置
1. AssetBundlesBrowser侵入式修改代码位置。有需要自己更新AssetBundlesBrowser的同学，请看此处
    - 首先，所有的AssetBundlesBrowser文件都存放在Assets/VersionUpdateBundle/AssetBundelsBrowser下
    - 和AssetBundlesBrowser交互的脚本文件是BundleAssetsMapMenuItems，在Assets/VersionUpdateBundle/VersionUpdateManager/Editor/BundleAssetsMapMenuItems.cs该文件有一共两个函数，一个是PrepareBundleAssetsMap，一个是BuildBundleCompleted。这两个都是需要修改AssetBundlesBrowser的AssetBundleBuildTab文件的。
        - PrepareBundleAssetsMap 是在点击Build按钮时调用的，用于收集项目的所有Bundle以及Bundle里面的所有资源，然后记录下每个资源的路径以及对应的Bundle，这样就可以使用完全透明的路径加载方式了。
        - BuildBundleCompleted 做一些收尾工作，删除临时创建的Bundle。
        - 下面图片是修改AssetBundleBuildTab文件的两处地方。

        ![Alt text](/ScreenShot/PrepareBundleAssetsMapCall.png)
        ![Alt text](/ScreenShot/BuildBundleCompletedCall.png)

2. 增量打包原理
    - 增量打包是记录下上个版本的Manifest文件，然后用新的Manifest文件，从中获得所有Bundle，对Bundle的Hash进行对比，如果发现Hash不同，或者这个Bundle是新增，则这个Bundle会被打到更新包里，所以增量打包是需要先打出所有的Bundle，然后才能进行对比。

3. VersionUpdateTable.json文件解析
    - 这个文件是每次打增量包都会生成的文件，如果是多次打包，这个文件是会自动合并的。
    - 此文件和打完包后的zip文件需要一起放到服务器上，客户端在版本更新检查时，会下载服务器的VersionUpdateTable.json文件，进行版本号对比，如果当前客户端版本低于服务器，则会下载并更新。
    - 下面是文件格式，这里面有两个版本，一个是1000，一个是10001，分别有1000.zip更新包和1001更新包，removeList是指该版本更新时需要删除的本地Bundle文件，这是在打增量包时，对比Manifest，如果新版本没有该Bundle，则会在此removeList里，等更新后，客户端本地的该Bundle文件将会被删除。
    ``` josn
    {
        "versions": [
            {
                "versionNumber": 1000,
                "packageList": [
                    "1000.zip"
                ],
                "removeList": []
            },
            {
                "versionNumber": 1001,
                "packageList": [
                    "1001.zip"
                ],
                "removeList": []
            }
        ]
    }
    ```

4. 关于SharpZibLib插件。使用这个插件主要是用来打增量包时，把Bundle打成Zip时使用。也是在版本更新时，从服务器上下载Zip文件，解压到本地时使用。


## License

Copyright (c) hubluesky Personal. All rights reserved.

Licensed under the [MIT](LICENSE.txt) License.
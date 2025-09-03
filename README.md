# XFramework

XFramework是一个通用的Unity游戏框架，旨在构建一套可复用、结构清晰、使用简单、功能全面的底层架构，实现了必要的管理器组件，并整合了一些好用的插件。

本架构的异步完全依赖于[UniTask](https://github.com/Cysharp/UniTask)（已经整合到项目中），资源管理器基于[YooAsset](https://github.com/tuyoogame/YooAsset)实现。

## 目录结构

```bash
Assets/
├── Configs/                       # 配置数据
│   ├── Classes/                   # 配置表类文件（生成）
│   └── Schemes/                   # 配置表数据文件
├── Example/                       # 示例游戏
├── Plugins/                       # 第三方插件
├── Res/                           # 游戏资源
├── Resources/                     # Unity Resources文件夹
│   ├── BillingMode.json           # YooAsset计费模式配置
│   └── YooAssetSettings.asset     # YooAsset设置
├── Settings/                      # 项目设置
│   ├── URP/                       # URP渲染管线设置
│   └── YooAsset/                  # YooAsset相关设置
└── XFramework/                    # 框架核心代码
    ├── Core/                      # 核心组件
    │   ├── GameLauncher.cs        # 游戏启动器（框架入口）
    │   ├── M.cs                   # 全局管理器访问类
    │   └── FrameworkComponents/   # 框架组件
    ├── Dependencies/              # 框架依赖库
    │   └── UniTask/               # UniTask
    ├── Editor/                    # 编辑器相关
    │   ├── Attributes/            # 自定义特性
    │   ├── EditorTools/           # 编辑器工具
    │   └── Inspectors/            # 自定义Inspector
    ├── Extensions/                # 扩展方法
    ├── Modules/                   # 框架模块（可选）
    ├── Settings/                  # 框架设置
    ├── Shaders/                   # 框架着色器（可选）
    ├── ThirdParty/                # 第三方集成（可选）
    └── Utils/                     # 工具类
```

## 框架设计

框架层包含多个必要的底层管理器，这些管理器都是普通 C# 类，GameLauncher 是唯一的 MonoBehaviour 单例，用来创建所有框架层的管理器。

管理器之间，难免会出现一个管理器依赖于另一个管理器功能的情况。为避免耦合（双向依赖），管理器之间又进行了层次划分，低层先创建和初始化，高层后创建和初始化。同时，底层管理器只向高层管理器提供服务，而绝对不可调用高层管理器功能。

依照这样的思想，管理器内部分层如下：

1. 基础层（Foundation，提供底层工具）：Utils、Algorithms、CachePool（通用池）、游戏设置
2. 内核层（Kernel，提供基础能力）：资源管理器、事件管理器、配置表管理器、对象池（Mono池）
3. 系统层（System，提供运行时服务）：UI 管理器、网络管理器、音频管理器 等
4. 游戏层（Game，实现玩法逻辑）：业务逻辑

```text
[GameLauncher]
      │
      ▼
┌───────────────┐
│   游戏层       │   ← 业务逻辑
│ (Game Layer)  │
└───────────────┘
          ▲
          │
┌───────────────┐
│   系统层       │   ← UI、网络、音频等
│ (System Layer)│
└───────────────┘
          ▲
          │
┌───────────────┐
│   内核层       │   ← 资源、事件、配置、对象池
│ (Kernel Layer)│
└───────────────┘
          ▲
          │
┌───────────────┐
│   基础层       │   ← 工具、算法、通用池
│(Foundation)   │
└───────────────┘
```
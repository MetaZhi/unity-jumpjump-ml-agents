>本文首发于“洪流学堂”微信公众号。  
洪流学堂，让你快人几步！

## 来给我投一票吧~
长按识别下方二维码或文章左下角阅读原文，在页面中点一下赞👍，感谢大家！  
https://connect.unity.com/p/jump-tiao-yi-tiao-with-ml-agents-wip  
PS：在微信中投票不用登陆注册哦~  
![](http://storage.hongliu.cc/18-1-27/60984382.jpg)

![](http://storage.hongliu.cc/model3_20180129192759.gif)

# 跳一跳介绍
最近微信上非常火的一个小游戏，相信大家都已经玩过了。

## 玩法
- 小人跳跃的距离和你按压屏幕的时长有关，按屏幕时间越长，跳的越远
- 跳到盒子上可以加分，没跳到盒子上游戏结束
- 连续跳到盒子中心可以成倍加分

# 开发历程
阅读此文章需要有一定的Unity3d基础和unity-ml-agents基础。  
文中有任何纰漏欢迎指正。

## 用Unity开发跳一跳
先参照微信原版用Unity3d开发了简版的跳一跳。  

视频教程 https://edu.csdn.net/course/detail/6975  
源码工程 https://github.com/zhenghongzhi/Unity-JumpJump

## PPO中一些名词解释
### Experience
每一次states，action及输出称为一次experience

### episode_length
指的是每一次游戏一个agent直到done所用的步数

### batch size
进行gradient descent的一个batch

### buffer size
先收集buffer size个数据，然后再计算进行gradient descent

### Number of Epochs
会对buffer size的数据处理几遍，比如为2，就会把这些数据处理2遍

### Time Horizon
被加入experience的条件是agent done或者收集到time horizon个的数据量才加入，为了搜集到更全的可能性，避免过拟合

## 模型迭代 
### 迭代1
一开始模型建立很困惑，官方的demo里并没有这种给一个输入以后，需要一段时间等结果才出来进行下一步的例子。  

如果使用2个按键分别表示按下和抬起的事件，并且训练出按键之间的联系，训练起来会很慢而且有可能无法拟合。  

一开始尝试用Player类型的brain，结果发现无法实现，只能用两个键的方式。然后使用Heuristic来直接模拟按键的时长，即action只有一个值，一个连续型的float，代表按空格的时长。  
这样的话只能去掉小人的缩放和台子的缩放效果。（后注：现在想来其实是有一个对应关系的，可以直接通过参数设置出来，但是没有过渡的动画。）  

这个模型用5个值作为state，分别是小人位置坐标的x，小人位置坐标的z，下一个盒子位置的x，下一个盒子位置的z，下一个盒子的localScale的x（x和z相同）  

在最开始的时候一直在考虑如果小人在空中的时候，action还一直输入，可能会在神经网络中建立出来奇怪的联系。所以本次迭代在小人在空中的时候如果给了大于0的action的时候给一个惩罚，reward -= 1**（这种想法其实是错误的）**  

开始训练...

#### 迭代1-1
经过2个小时的训练发现效果很不好
禁止输入应该是一个规则，而不应该惩罚玩家，即使玩家输入游戏也不应该有反馈，所以去掉了小人在空中时候的惩罚。  

另外用Academy中的Frame to Skip参数，设为30，来减少无用的输入。

开始训练...

#### 迭代1-2
游戏出现了卡死的情况，游戏中的判定有BUG，导致可以一直接收action并且没有获得reward。  
修复BUG...开始训练...

#### 迭代1-3
训练效果一直不好，又重新将unity-ml-agents里面的文档都看了一遍，特别参照best practice将参数重新调整了一遍开始训练。  
训练了一个晚上，大概8个小时，效果依然不好...

### 迭代2
本来想着这么简单的模型，训练出来一定的成果再用curriculum的方式重新训练对比以下，看来只能直接上Curriculum看看效果了...
定义了如下的Curriculum文件

```
{
    "measure" : "reward",
    "thresholds" : [10,10,10,10,10,10,10,10,10,10,10,10,10,10,10],
    "min_lesson_length" : 2,
    "signal_smoothing" : true, 
    "parameters" : 
    {
        "max_distance" : [1.1,1.1,1.1,1.1,1.5,1.5,1.5,1.5,2,2,2,2,3,3,3,3],
        "min_scale" :    [1,1,0.5,0.5,1,1,0.5,0.5,1,1,0.5,0.5,1,1,0.5,0.5],
        "random_direction":[0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1]
    }
}
```
>这里需要注意的是，thresholds的值要比下面parameters里面的值少一个，因为最后一个lesson是会一直训练下去的，没有threshold

#### 迭代2-1
忽然游戏崩了，发现log是场景里collider太多了  
这次训练的效果还不错，但是游戏里还是有bug，导致box生成太多了  
将box的生成方式改为对象池的方式，性能能优化不少，也应该不会有刚才的bug了

#### 迭代2-2
嗯，这么训练确实不错，tensorboard里的cumulative_reward一直在增长，但是训练了一段时间以后想到一个可能存在的问题，由于第一个max_distance是1.1，和最小distance是一样的，也就是说下一个盒子总是距离现在的盒子1.1米，可能会有过拟合的问题，而且threshold貌似有点太大了，一直没有切换到下一个lesson  

于是赶紧停掉训练，将第一个max_distance改成了1.2重新训练

#### 迭代2-3
发现了不会切换lesson的问题，赶紧去查,在issues里面发现了答案，只有global_done为true时才会切换，需要将academy设为done或者设置一个max steps  
这次将每次game over的时候academy设置为done  
后来发现这样其实是有点问题的，write_summary之后cumulative_reward会清空，如果刚好这之后的cumulative_reward波动出来大的值，那么就会跳过lesson，这样随机性很大。怪不得官方的demo用的是max step 50000

#### 迭代2-4
经过30小时的训练，第一版终于训练出来了，但是结果很不理想  
![](http://storage.hongliu.cc/18-1-26/76109390.jpg)  
如图：
1. 第一个过程就是开始设置max_distance是1.1的时候，增长的很快
2. 修改为1.2之后，又训练了一段时间，发现不会切换lesson，然后改了bug
3. 后面lesson切换的很快
4. 最后训练了很久，但是cumulative_reward也没到1

于是开始反思模型上的问题，要简化模型，优化Curriculum。

### 迭代3
将state重新定义为2个，一个是小人与下一个盒子之间的距离，一个是下一个盒子的size
Curriculum也重新定义为：

```
{
    "measure" : "reward",
    "thresholds" : [5,5,5,5,5,5,5,5,5,5,5,5,5,5,5],
    "min_lesson_length" : 2,
    "signal_smoothing" : true, 
    "parameters" : 
    {
        "max_distance" : [1.2,1.2,1.2,1.2,1.5,1.5,1.5,1.5,2,2,2,2,3,3,3,3],
        "min_scale" :    [1,0.9,0.7,0.5,1,0.9,0.7,0.5,1,0.9,0.7,0.5,1,0.9,0.7,0.5],
        "random_direction":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]
    }
}
```
由于现在state中是小人与下一个盒子之间的距离，而生成与跳跃方向又是游戏系统定的，所以random direction就没有什么用了。
而且问了能加快训练速度，重构了游戏和场景，使得可以有很多游戏instance可以一起训练，去掉random direction可以避免游戏之间发生冲突
![](http://storage.hongliu.cc/18-1-26/69927979.jpg)

#### 迭代3-1
出现了两次Info/cumulative_reward突然剧烈下降并且再也回不去的情况，虽然这两次发生的时候都进行了别的工程webgl的编译，但是应该还是参数的问题
1. 怀疑是layer为1的锅，虽然模型很简单，因为并不是线性关系那么简单，所以把layer加为2重新训练  
1. 看来不是layer为1的锅。怀疑原地跳的话-0.1的惩罚有点太大了，改为-0.01重新训练试一下  
1. 再次训练还是出现了剧烈下降，这次直接删掉原地跳的惩罚，再试验看看是什么问题  
1. 还是会出现剧烈下降，试着寻找问题的原因  
1. 修改了以下内容：
    - 去掉normalization，改为手动normalization
    - academy max steps改为1000

还是不行！！！！！！！！！！！！！！！

开始怀疑是多个instance同时运行出现的问题，但是没找到多个instance同时运行的逻辑问题

经过反复思考，得到是游戏机制问题。小人跳跃时不应该接收输入，否则的话前一个step的输入得到的reward值会是0，无法对应起来。之前设置的skipfram30确实有问题，无法将states，action和reward对应起来  

2018年1月27日23点19分  
但是经过debug发现，有超过200帧还无法接收下次输入的情况，高达5000帧，这样就没法用skip frame这个值了，训练速度会太慢太慢，怀疑程序有bug，调试中。。。  

发现高达5000帧时，有player掉到地面以下的情况，可能是物理穿透，，加上一个y位置的判断，如果y的值小于1，那么表示小人穿透了地面gameover，并且将ground的box厚度改大  

而且发现frame skip 有bug，还是我没理解对，竟然每帧都会执行step  
Edit:
I find in Academy.cs line:345 that following code is not surrounded by `if (skippingFrames == false)`.  
Am I misunderstanding something?
```
AcademyStep();

foreach (Brain brain in brains)
{
     brain.Step();
}
```
在github上发了个issuse，寻求建议  
重新调试测试，用代码的方式检测skipframe的最佳值，设为了200  
是否跳到盒子上端的检测方式改为法线检测  
并且将上述的代码加到了if判断里面

#### 迭代3-2
2018年1月28日  
今天早上起来发现模型还是不行，而且游戏里还有bug  
14点29分  
在github上发了个issuse，作者给了个建议，可以使用unsubscribe和resubscribe的方式来让小人在空中的时候不接受action
但是测试后发现不行，因为动态unsubscribe和resubscribe的话，agents数量是动态变化的，python端上次输入和这次输出的信息，可能长度不相等  
输入的时候可能有10个小人的信息，但是输出的时候只有8个或20个  

修复了游戏里的bug：跳到了下一个盒子侧面，又和当前的盒子发生了碰撞，会造成逻辑问题  

最终解决方案还是得用frame skip，这次游戏运行稳定了，通过统计找到一个最佳值是137，而且小人每一步都不会超过137帧。将场景中game instance的数量设置为100个，layer 1，hidden units 32  
开始训练

发现一个报错，game instance数量太多，导致socket buffer超出的问题，复现了几次，出现问题时收到的buffer长度都是1460或者1460的倍数。然后意识到这是由于socket的包分包所致

>路由器有一个MTU（ 最大传输单元），一般是1500字节，除去IP头部20字节，留给TCP的就只有MTU-20字节。所以一般TCP的MSS为MTU-20=1460字节。

还是会出现剧烈下降的问题，应该是learning rate的问题，learning rate太大了，降低learning rate到1e-4  
终于训练成功了，但是跳到中心的几率不高，感觉需要修改reward，给跳到中心加分多一些 试试中心+1，台子上+0.1试试

#### 迭代3-3
2018年1月29日13点21分  
终于训练出比较成功的模型了，当前设置了小人最多跳100步，所以cumulative_reward最好也就是100  
```
Step: 349000. Mean Reward: 88.70098911968347. Std of Reward: 10.67634076995865.
Saved Model
Step: 350000. Mean Reward: 89.22838773491591. Std of Reward: 9.210647049058082.
Saved Model
Step: 351000. Mean Reward: 89.44072978303747. Std of Reward: 12.138629221972357.
Saved Model
```
其实所有东西都搞对以后训练的速度还是挺快的，大概到200K步的时候Mean Reward就能到70了

### 后记
搞完以后尝试修复socket分包的问题，修复完创建PR的时候发现：
>development-0.3的分支中已经修复这个问题  

哈哈，白费功夫了~不过对python的socket通信以及粘包有了更深的了解

# 总结
这个游戏也算是开创了一个mg-agents的游戏类型：需要等待一些时间才能得到reward的情况。  
虽然最终是用skip frame解决的，但是中间做了很多的探索，也发现了ml-agents的一些局限。  
**贡献**：在GitHub上提了一个pull request（解决了env含有子目录时export_graph的bug）和2个issue（一个是寻求这种类型游戏的建议——作者回复会跟进这种类型游戏的支持；一个是unity端解析socket buffer的json失败的问题，自己修复了这个问题，不过development-0.3的分支中已经修复这个问题）。  
自己在Unity3d的一些细节开发，增强学习、ml-agents的使用和理解上也有不小的进步。  
共同交流，共勉~

---
![](http://storage.hongliu.cc/18-1-30/69476971.jpg)

---
交流群：492325637  
**关注“洪流学堂”微信公众号，让你快人几步**   
![](http://storage.hongliu.cc/17-12-22/9925227.jpg)  

![](http://storage.hongliu.cc/18-1-30/35132192.jpg)

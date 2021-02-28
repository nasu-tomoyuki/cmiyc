# Catch Me If You Can.
## これは？
ゲームでもなんでもありません。

キャラクターが自律的に動くのを眺めるのが好きなので作りました。


## ルール
1. ここはパパのりんご園です。ユニティちゃんはパパの目を盗んでりんごを食べています。
1. ユニティちゃんは若くてかわいいので農園全体が見えています。パパは歳なので視野は狭いし、あまり遠くは見えないし、壁の向こうも見えません。
1. パパがユニティちゃんを捕まえると世界はリセットされます。
1. 地面をクリックするとお酒が出てきます。同じ場所をクリックすると消えます。
1. パパはお酒が大好きなので見つけたらまっしぐら。飲んで寝てしまいます。
1. なぜか一定時間ごとにパパは分裂して増えていきます。


## 動作デモ
![デモ](demo.png)

* このページで操作できます
  * https://nasu-tomoyuki.github.io/cmiyc/


## かんたんな解説
### パパ
ランダムに移動して、視界に入ったら A* して追いかけます。

### ユニティちゃん
影響マップを見て影響度の低い場所を探しています。

パパの周囲の影響度を上げて、りんごの位置の影響度を下げています。結果、パパを避けてりんごの場所を目指します。


## ライセンス
ソースコードについては [MIT LICENSE](License/LICENSE) です。


## アセット
次のデジタルアセットデータを利用しています。
それぞれのライセンが適用されます。

### ユニティちゃん SD大鳥ゆうじ
* © Unity Technologies Japan/UCL
* https://unity-chan.com/

### ローポリリンゴ 
* https://booth.pm/ja/items/1264675

### 日本酒9本飲み比べセット 3Dモデル
* https://booth.pm/ja/items/1226387


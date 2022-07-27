# RESTful Feed Filter

## これは何

RSSやAtomなどのフィードから不要な要素を除外する簡易APIです。不要な要素はXPathで指定します。

## デモサイト

### URL

https://rff.azurewebsites.net/

### 注意点

* デモサイトはAzureの無料プランで動いているため、無料枠を使い切ると一時的に落ちます
* 生成したURLが動作するかはRSSリーダ次第です
    * Inoreaderでは動作しました
    * FeedlyではRSS1.0のフィードが認識しません

## 使い方

1. URLを開くとSwagger UIが表示されるため、以下2つを指定してください
    * 元となるフィードのURL
    * 除外したい要素を選択するXPath
1. いい感じに絞り込めるXPathが完成したら`Request URL`の所に表示されたURLをRSSリーダに登録してください

## XPathについて

### Namespace関連

* 元のXMLに定義されているxmlnsは、XPath側でも同じプレフィックスで使用可能です
* 元のXMLにプレフィックスなしのデフォルトxmlnsが定義されている場合、XPath側で`default`プレフィックスをつけることでマッチします
    * [プレフィックス無しのXPathは「Namespace無し」と解釈される仕様のため](https://docs.microsoft.com/ja-jp/dotnet/api/system.xml.xmlnode.selectnodes?view=net-6.0#remarks)、元XMLにデフォルトxmlnsがある場合にプレフィックス無しXPathを指定すると何もマッチしません。この仕様への対策のため、元XMLにデフォルトxmlnsがあれば、同じものをXPath側に`default`プレフィックスで定義しています
    * Atomフィード(親要素がfeedのもの)、RSS1.0フィード(親要素がrdfのもの)ではデフォルトxmlnsが定義されているため注意してください
    * RSS2.0フィード(親要素がrssのもの)ではこれらの考慮は不要です

### XPathサンプル

* タイトルに`【PR】`が含まれているものを除外する
    * RSS2.0: `/rss/channel/item[contains(title,'【PR】')]`
    * RSS1.0: `/rdf:RDF/default:item[contains(default:title,'【PR】'))]`
    * Atom: `/default:feed/default:entry[contains(default:title, '【PR】')]`
* categoryタグの値が`event`でないものを除外する
    * RSS2.0: `/rss/channel/item[not(category/text()='event')]`
* URLに`event`と`news`のどちらも含まれないものを除外する
    * RSS2.0: `/rss/channel/item[not(contains(link,'event') or contains(link,'news'))]`

# AI
角色AI

## [PlayMaker 控制之AI](https://github.com/MomomoRu/AI/tree/main/PlayMakerBehaviour)

使用 PlayMaker 控制的 AI，在每一個狀態下會有多種功能和細節需要處理，我將各狀態所需要處理的內容製作為對應的 Behavior，編輯 PlayMaker AI 邏輯的企劃人員只需要擺放和控制 Behavior 的切換即可便捷的編輯出角色 AI。

BehaviorBase:Behavior的基礎類別

BehaviorIdle:Idle狀態

BehaviorMove:移動行為的基礎類別

BehaviorGoToTarget:走向Target之狀態

BehaviorCustomMove:以Target為中心，在Target周圍走動的移動模式

BehaviorAttack:攻擊狀態

BehaviorRotateAnchor、BehaviorRotateTo、BehaviorRotateBack:旋轉控制

## [直接以程式邏輯控制之AI](https://github.com/MomomoRu/AI/tree/main/AIBrain)

### 目的
為了簡化角色的效能開銷，把基礎的 AI 的行為用程式架構出來，將行為比較制式的AI角色改以程式控制，減少 PlayMaker 的效能開銷。

AIEnum、AnimatorStateEnum:AI使用到的相關enum定義

CharacterLocomotion:控制角色 Animator

BehaviorFunc:提供操作角色移動、面向、距離判定的函式庫

BrainBehavior:角色某狀態的行為

AIBrain:角色AI邏輯核心，判斷條件操控狀態的切換

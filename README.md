# 🌵 Western Frontier — 3D Action RPG (Unity)

> **Western Frontier** là đồ án game 3D thuộc thể loại Action RPG được phát triển bằng Unity 6 & C#. Game tập trung vào cơ chế chiến đấu (Combat System), hệ thống trang bị (Equipment), nhiệm vụ (Quest) và Enemy AI.

---

## 🎬 Gameplay Demo & Media
* **Video Gameplay Trailer:** https://youtu.be/Fe95LeweFbg
 
---

## ⚙️ Key Technical Features

### ⚔️ Combat System
* **3-Phase Melee Combat:** Thiết kế luồng tấn công chuẩn 3 pha (*WindUp - Active - Recovery*) kết hợp **Input Buffering** giúp chuỗi combo mượt mà.
* **Hit Detection:** Sử dụng `SphereCastNonAlloc` tối ưu hiệu năng để tính toán va chạm vũ khí chính xác.

### 🧠 Enemy AI & Boss Mechanics
* **Finite State Machine (FSM):** Quản lý trạng thái hành vi của quái thường và Boss (Idle, Patrol, Chase, Attack, Hurt, Dead).
* **Boss Fights:** Thiết kế cơ chế Boss Mage Skeleton với các kỹ năng đòn đánh tầm xa, triệu hồi AoE và gọi sét.

### 📜 Game Systems & Architecture
* **Event-Driven Architecture:** Xây dựng `GameEvents` decoupled để giao tiếp giữa các hệ thống (Quest, Inventory, UI) mà không bị phụ thuộc code (tight coupling).
* **Inventory & Equipment System:** Hệ thống 6 slot trang bị tự động tính toán lại chỉ số nhân vật (ATK, DEF, Speed) theo dạng ScriptableObject.
* **Quest & Travel System:** Quản lý tiến trình nhiệm vụ và chuyển Scene (Travel) lưu giữ trạng thái dữ liệu nhân vật.

### ⚡ Optimization & Polish
* **Object Pooling:** Tái sử dụng VFX đòn đánh, đạn và hiệu ứng môi trường để giảm phân mảnh bộ nhớ (GC Alloc).
* **Cinemachine & Audio:** Tích hợp Cinemachine Camera Shake khi va chạm đòn đánh và hệ thống âm thanh SFX/BGM sinh động.

---

## 🛠️ Tech Stack & Tools
* **Engine:** Unity 6 (URP - Universal Render Pipeline)
* **Language:** C#
* **Navigation:** Unity NavMesh
* **Camera & UI:** Cinemachine, TextMeshPro
* **Version Control:** Git

---

## 👤 Author
* **Developer:** Dư Đức Thành
* **Email:** phatduduc@gmail.com
* **LinkedIn:** https://www.linkedin.com/in/d%C6%B0-%C4%91%E1%BB%A9c-th%C3%A0nh-925905427/


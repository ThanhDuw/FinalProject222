using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CreatorKitCodeInternal {
    /// <summary>
    /// Các Monobehaviour đơn giản được dùng để lấy nhanh tham chiếu đến Image và Slider được dùng bởi biểu tượng hiệu ứng (effect icon) đang hoạt động trên UI.
    /// </summary>
    public class EffectIconUI : MonoBehaviour
    {
        public Image BackgroundImage;
        public Slider TimeSlider;
    }
}
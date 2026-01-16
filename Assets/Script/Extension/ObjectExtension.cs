using UnityEngine;

namespace Hunt
{
    public static class ObjectExtension
    {
        /// <summary> 컴포넌트 초기화 검증 및 로그 출력 </summary>
        public static T ValidInit<T>(this T obj, string name = null) where T : class
        {
            string componentName = name ?? typeof(T).Name;

            if (obj == null)
            {
                $"{componentName}을 찾을 수 없습니다".DError();
            }
            else
            {
                $"[Init] {componentName} 초기화".DLog();
            }

            return obj;
        }

        /// <summary> 컴포넌트가 null인지만 체크하고 에러 로그 </summary>
        public static bool IsNull<T>(this T obj, string name = null) where T : class
        {
            if (obj == null)
            {
                string componentName = name ?? typeof(T).Name;
                $"{componentName}이(가) null입니다".DError();
                return true;
            }
            return false;
        }
    }


}
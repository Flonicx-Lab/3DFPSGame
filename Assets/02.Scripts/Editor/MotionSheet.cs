using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FpsGame.Editor
{
    // 애니메이션 상태의 시간별 화면과 자세 수치를 한 쌍의 파일로 기록한다.
    public static class MotionSheet
    {
        private const int CellWidth = 480;
        private const int CellHeight = 270;
        private const int FrameWaitCount = 2;
        private const int DigitWidth = 3;
        private const int DigitHeight = 5;
        private const int DigitScale = 2;
        private const int DigitSpacing = 1;

        private static readonly Color32 TrajectoryColor = new(255, 220, 0, 255);
        private static readonly Color32 CurrentPointColor = new(255, 0, 0, 255);
        private static readonly Color32 CenterColor = new(0, 255, 0, 255);
        private static readonly Color32 NumberColor = new(255, 255, 255, 255);
        private static readonly Color32 EmptyCellColor = new(0, 0, 0, 255);
        private static readonly int[,] DigitRows =
        {
            { 7, 5, 5, 5, 7 },
            { 2, 6, 2, 2, 7 },
            { 7, 1, 7, 4, 7 },
            { 7, 1, 7, 1, 7 },
            { 5, 5, 7, 1, 1 },
            { 7, 4, 7, 1, 7 },
            { 7, 4, 7, 5, 7 },
            { 7, 1, 1, 1, 1 },
            { 7, 5, 7, 5, 7 },
            { 7, 5, 7, 1, 7 },
        };

        private static readonly List<Color32[]> CellPixels = new();
        private static readonly List<Sample> Samples = new();

        private static Animator _animator;
        private static Camera _camera;
        private static Transform _effector;
        private static Transform _hips;
        private static Transform _neck;
        private static Transform _leftShoulder;
        private static Transform _rightShoulder;
        private static Transform _leftUpperArm;
        private static Transform _leftLowerArm;
        private static Transform _leftHand;
        private static Transform _rightUpperArm;
        private static Transform _rightLowerArm;
        private static Transform _rightHand;
        private static Transform _leftUpperLeg;
        private static Transform _leftLowerLeg;
        private static Transform _leftFoot;
        private static Transform _rightUpperLeg;
        private static Transform _rightLowerLeg;
        private static Transform _rightFoot;
        private static Vector3 _effectorLocalPoint;
        private static int _layer;
        private static string _stateName;
        private static int _sampleCount;
        private static int _columns;
        private static string _pngPath;
        private static string _csvPath;
        private static float _savedTimeScale;
        private static int _sampleStartFrame;
        private static int _sampleIndex;
        private static float _clipLength;
        private static float _clipFrameRate;
        private static bool _isCaptureActive;

        public static void Capture(
            Animator animator,
            int layer,
            string stateName,
            Transform effector,
            Vector3 effectorLocalPoint,
            int sampleCount,
            int columns,
            string outputPathWithoutExtension)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("MotionSheet 캡처는 플레이 모드에서만 실행할 수 있습니다.");
            }

            ValidateArguments(
                animator,
                layer,
                stateName,
                effector,
                sampleCount,
                columns,
                outputPathWithoutExtension);
            InitializeCapture(
                animator,
                layer,
                stateName,
                effector,
                effectorLocalPoint,
                sampleCount,
                columns,
                outputPathWithoutExtension);

            try
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _isCaptureActive = true;
                EditorApplication.update += OnEditorUpdate;
                PlaySample(0);
            }
            catch
            {
                StopCapture();
                throw;
            }
        }

        private static void ValidateArguments(
            Animator animator,
            int layer,
            string stateName,
            Transform effector,
            int sampleCount,
            int columns,
            string outputPathWithoutExtension)
        {
            if (_isCaptureActive)
            {
                throw new InvalidOperationException("MotionSheet 캡처가 이미 진행 중입니다.");
            }

            if (animator == null)
            {
                throw new ArgumentNullException(nameof(animator));
            }

            if (!animator.isHuman)
            {
                throw new ArgumentException("Animator는 Humanoid Avatar를 사용해야 합니다.", nameof(animator));
            }

            if (layer < 0 || layer >= animator.layerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(layer));
            }

            if (string.IsNullOrWhiteSpace(stateName))
            {
                throw new ArgumentException("상태 이름이 비어 있습니다.", nameof(stateName));
            }

            if (effector == null)
            {
                throw new ArgumentNullException(nameof(effector));
            }

            if (sampleCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount), "샘플 수는 2 이상이어야 합니다.");
            }

            if (columns < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(columns), "열 수는 1 이상이어야 합니다.");
            }

            if (string.IsNullOrWhiteSpace(outputPathWithoutExtension))
            {
                throw new ArgumentException("출력 경로가 비어 있습니다.", nameof(outputPathWithoutExtension));
            }

            if (Camera.main == null)
            {
                throw new InvalidOperationException("MainCamera 태그가 지정된 카메라가 없습니다.");
            }
        }

        private static void InitializeCapture(
            Animator animator,
            int layer,
            string stateName,
            Transform effector,
            Vector3 effectorLocalPoint,
            int sampleCount,
            int columns,
            string outputPathWithoutExtension)
        {
            _animator = animator;
            _camera = Camera.main;
            _effector = effector;
            _effectorLocalPoint = effectorLocalPoint;
            _layer = layer;
            _stateName = stateName;
            _sampleCount = sampleCount;
            _columns = columns;
            _pngPath = Path.GetFullPath(outputPathWithoutExtension + ".png");
            _csvPath = Path.GetFullPath(outputPathWithoutExtension + ".csv");
            _sampleIndex = 0;
            _clipLength = 0f;
            _clipFrameRate = 0f;
            CellPixels.Clear();
            Samples.Clear();
            CacheBones(animator);
        }

        private static void CacheBones(Animator animator)
        {
            _hips = GetRequiredBone(animator, HumanBodyBones.Hips);
            _neck = GetRequiredBone(animator, HumanBodyBones.Neck);
            _leftShoulder = GetRequiredBone(animator, HumanBodyBones.LeftShoulder);
            _rightShoulder = GetRequiredBone(animator, HumanBodyBones.RightShoulder);
            _leftUpperArm = GetRequiredBone(animator, HumanBodyBones.LeftUpperArm);
            _leftLowerArm = GetRequiredBone(animator, HumanBodyBones.LeftLowerArm);
            _leftHand = GetRequiredBone(animator, HumanBodyBones.LeftHand);
            _rightUpperArm = GetRequiredBone(animator, HumanBodyBones.RightUpperArm);
            _rightLowerArm = GetRequiredBone(animator, HumanBodyBones.RightLowerArm);
            _rightHand = GetRequiredBone(animator, HumanBodyBones.RightHand);
            _leftUpperLeg = GetRequiredBone(animator, HumanBodyBones.LeftUpperLeg);
            _leftLowerLeg = GetRequiredBone(animator, HumanBodyBones.LeftLowerLeg);
            _leftFoot = GetRequiredBone(animator, HumanBodyBones.LeftFoot);
            _rightUpperLeg = GetRequiredBone(animator, HumanBodyBones.RightUpperLeg);
            _rightLowerLeg = GetRequiredBone(animator, HumanBodyBones.RightLowerLeg);
            _rightFoot = GetRequiredBone(animator, HumanBodyBones.RightFoot);
        }

        private static Transform GetRequiredBone(Animator animator, HumanBodyBones bone)
        {
            Transform boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform == null)
            {
                throw new InvalidOperationException($"Humanoid Avatar에 {bone} 뼈가 없습니다.");
            }

            return boneTransform;
        }

        private static void PlaySample(int sampleIndex)
        {
            _sampleIndex = sampleIndex;
            float normalizedTime = sampleIndex / (float)(_sampleCount - 1);
            _animator.Play(_stateName, _layer, normalizedTime);
            _sampleStartFrame = Time.frameCount;
        }

        private static void OnEditorUpdate()
        {
            if (!_isCaptureActive || Time.frameCount - _sampleStartFrame < FrameWaitCount)
            {
                return;
            }

            try
            {
                CaptureCurrentSample();
                int nextSampleIndex = _sampleIndex + 1;
                if (nextSampleIndex < _sampleCount)
                {
                    PlaySample(nextSampleIndex);
                    return;
                }

                WriteOutputs();
                string pngPath = _pngPath;
                string csvPath = _csvPath;
                StopCapture();
                Debug.Log($"MotionSheet PNG 저장 완료: {pngPath}\nMotionSheet CSV 저장 완료: {csvPath}");
            }
            catch
            {
                StopCapture();
                throw;
            }
        }

        private static void CaptureCurrentSample()
        {
            float normalizedTime = _sampleIndex / (float)(_sampleCount - 1);
            if (_sampleIndex == 0)
            {
                CacheClipTiming();
            }

            Vector3 effectorWorldPosition = _effector.TransformPoint(_effectorLocalPoint);
            Vector3 viewportPosition = _camera.WorldToViewportPoint(effectorWorldPosition);
            float movementSpeed = CalculateMovementSpeed(effectorWorldPosition);
            Vector3 rightAxis = (_rightShoulder.position - _leftShoulder.position).normalized;
            Vector3 upAxis = Vector3.ProjectOnPlane(_neck.position - _hips.position, rightAxis).normalized;
            Vector3 forwardAxis = Vector3.Cross(rightAxis, upAxis).normalized;

            Sample sample = new Sample
            {
                Number = _sampleIndex + 1,
                NormalizedTime = normalizedTime,
                ClipFrame = Mathf.RoundToInt(normalizedTime * _clipLength * _clipFrameRate),
                EffectorWorldPosition = effectorWorldPosition,
                ViewportPosition = viewportPosition,
                MovementSpeed = movementSpeed,
                LeftElbowAngle = CalculateJointAngle(_leftUpperArm, _leftLowerArm, _leftHand),
                RightElbowAngle = CalculateJointAngle(_rightUpperArm, _rightLowerArm, _rightHand),
                LeftKneeAngle = CalculateJointAngle(_leftUpperLeg, _leftLowerLeg, _leftFoot),
                RightKneeAngle = CalculateJointAngle(_rightUpperLeg, _rightLowerLeg, _rightFoot),
                RightHandBodyPosition = CalculateBodyPosition(
                    _rightHand.position,
                    _rightShoulder.position,
                    rightAxis,
                    upAxis,
                    forwardAxis),
                LeftHandBodyPosition = CalculateBodyPosition(
                    _leftHand.position,
                    _leftShoulder.position,
                    rightAxis,
                    upAxis,
                    forwardAxis),
                LeftFootHeight = _leftFoot.position.y - _animator.transform.position.y,
                RightFootHeight = _rightFoot.position.y - _animator.transform.position.y,
            };

            Samples.Add(sample);
            CellPixels.Add(RenderCell());
        }

        private static void CacheClipTiming()
        {
            AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(_layer);
            if (clipInfos.Length == 0)
            {
                throw new InvalidOperationException("지정한 상태에서 재생 중인 AnimationClip을 찾을 수 없습니다.");
            }

            AnimatorClipInfo selectedClipInfo = clipInfos[0];
            for (int i = 1; i < clipInfos.Length; i++)
            {
                if (clipInfos[i].weight > selectedClipInfo.weight)
                {
                    selectedClipInfo = clipInfos[i];
                }
            }

            _clipLength = selectedClipInfo.clip.length;
            _clipFrameRate = selectedClipInfo.clip.frameRate;
            if (_clipLength <= 0f || _clipFrameRate <= 0f)
            {
                throw new InvalidOperationException("AnimationClip의 길이와 프레임레이트는 0보다 커야 합니다.");
            }
        }

        private static float CalculateMovementSpeed(Vector3 effectorWorldPosition)
        {
            if (Samples.Count == 0)
            {
                return 0f;
            }

            float sampleDuration = _clipLength / (_sampleCount - 1);
            Vector3 previousPosition = Samples[Samples.Count - 1].EffectorWorldPosition;
            return Vector3.Distance(previousPosition, effectorWorldPosition) / sampleDuration;
        }

        private static float CalculateJointAngle(Transform first, Transform joint, Transform third)
        {
            Vector3 firstDirection = first.position - joint.position;
            Vector3 thirdDirection = third.position - joint.position;
            return Vector3.Angle(firstDirection, thirdDirection);
        }

        private static Vector3 CalculateBodyPosition(
            Vector3 worldPosition,
            Vector3 origin,
            Vector3 rightAxis,
            Vector3 upAxis,
            Vector3 forwardAxis)
        {
            Vector3 offset = worldPosition - origin;
            return new Vector3(
                Vector3.Dot(offset, rightAxis),
                Vector3.Dot(offset, upAxis),
                Vector3.Dot(offset, forwardAxis));
        }

        private static Color32[] RenderCell()
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(CellWidth, CellHeight, 24);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = _camera.targetTexture;
            Texture2D cellTexture = null;

            try
            {
                _camera.targetTexture = renderTexture;
                _camera.Render();
                RenderTexture.active = renderTexture;
                cellTexture = new Texture2D(CellWidth, CellHeight, TextureFormat.RGB24, false);
                cellTexture.ReadPixels(new Rect(0f, 0f, CellWidth, CellHeight), 0, 0);
                cellTexture.Apply();
                return cellTexture.GetPixels32();
            }
            finally
            {
                _camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
                if (cellTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(cellTexture);
                }
            }
        }

        private static void WriteOutputs()
        {
            string directoryPath = Path.GetDirectoryName(_pngPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            WritePng();
            WriteCsv();
        }

        private static void WritePng()
        {
            int rowCount = Mathf.CeilToInt(_sampleCount / (float)_columns);
            int sheetWidth = _columns * CellWidth;
            int sheetHeight = rowCount * CellHeight;
            Color32[] sheetPixels = new Color32[sheetWidth * sheetHeight];
            for (int i = 0; i < sheetPixels.Length; i++)
            {
                sheetPixels[i] = EmptyCellColor;
            }

            for (int i = 0; i < CellPixels.Count; i++)
            {
                DrawCellOverlay(CellPixels[i], i);
                CopyCellToSheet(CellPixels[i], sheetPixels, i, rowCount, sheetWidth);
            }

            Texture2D sheetTexture = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            try
            {
                sheetTexture.SetPixels32(sheetPixels);
                sheetTexture.Apply();
                File.WriteAllBytes(_pngPath, sheetTexture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheetTexture);
            }
        }

        private static void DrawCellOverlay(Color32[] pixels, int currentSampleIndex)
        {
            for (int i = 1; i < Samples.Count; i++)
            {
                Vector3 startViewport = Samples[i - 1].ViewportPosition;
                Vector3 endViewport = Samples[i].ViewportPosition;
                DrawViewportLine(pixels, startViewport, endViewport, TrajectoryColor);
            }

            Vector3 currentViewport = Samples[currentSampleIndex].ViewportPosition;
            int currentX = Mathf.RoundToInt(currentViewport.x * (CellWidth - 1));
            int currentY = Mathf.RoundToInt(currentViewport.y * (CellHeight - 1));
            DrawCross(pixels, CellWidth / 2, CellHeight / 2, 4, CenterColor);
            DrawCircle(pixels, currentX, currentY, 4, CurrentPointColor);
            DrawNumber(pixels, Samples[currentSampleIndex].Number, 6, CellHeight - 6, NumberColor);
        }

        private static void DrawViewportLine(
            Color32[] pixels,
            Vector3 startViewport,
            Vector3 endViewport,
            Color32 color)
        {
            float startX = startViewport.x * (CellWidth - 1);
            float startY = startViewport.y * (CellHeight - 1);
            float endX = endViewport.x * (CellWidth - 1);
            float endY = endViewport.y * (CellHeight - 1);
            if (!ClipLineToCell(ref startX, ref startY, ref endX, ref endY))
            {
                return;
            }

            int stepCount = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(endX - startX), Mathf.Abs(endY - startY)));
            if (stepCount == 0)
            {
                SetPixel(pixels, Mathf.RoundToInt(startX), Mathf.RoundToInt(startY), color);
                return;
            }

            for (int i = 0; i <= stepCount; i++)
            {
                float ratio = i / (float)stepCount;
                int x = Mathf.RoundToInt(Mathf.Lerp(startX, endX, ratio));
                int y = Mathf.RoundToInt(Mathf.Lerp(startY, endY, ratio));
                SetPixel(pixels, x, y, color);
                SetPixel(pixels, x + 1, y, color);
            }
        }

        private static bool ClipLineToCell(ref float startX, ref float startY, ref float endX, ref float endY)
        {
            float deltaX = endX - startX;
            float deltaY = endY - startY;
            float minimumRatio = 0f;
            float maximumRatio = 1f;

            if (!ClipLineBoundary(-deltaX, startX, ref minimumRatio, ref maximumRatio)
                || !ClipLineBoundary(deltaX, CellWidth - 1 - startX, ref minimumRatio, ref maximumRatio)
                || !ClipLineBoundary(-deltaY, startY, ref minimumRatio, ref maximumRatio)
                || !ClipLineBoundary(deltaY, CellHeight - 1 - startY, ref minimumRatio, ref maximumRatio))
            {
                return false;
            }

            endX = startX + maximumRatio * deltaX;
            endY = startY + maximumRatio * deltaY;
            startX += minimumRatio * deltaX;
            startY += minimumRatio * deltaY;
            return true;
        }

        private static bool ClipLineBoundary(
            float direction,
            float distance,
            ref float minimumRatio,
            ref float maximumRatio)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return distance >= 0f;
            }

            float ratio = distance / direction;
            if (direction < 0f)
            {
                if (ratio > maximumRatio)
                {
                    return false;
                }

                minimumRatio = Mathf.Max(minimumRatio, ratio);
                return true;
            }

            if (ratio < minimumRatio)
            {
                return false;
            }

            maximumRatio = Mathf.Min(maximumRatio, ratio);
            return true;
        }

        private static void DrawCircle(Color32[] pixels, int centerX, int centerY, int radius, Color32 color)
        {
            int squaredRadius = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= squaredRadius)
                    {
                        SetPixel(pixels, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void DrawCross(Color32[] pixels, int centerX, int centerY, int radius, Color32 color)
        {
            for (int offset = -radius; offset <= radius; offset++)
            {
                SetPixel(pixels, centerX + offset, centerY, color);
                SetPixel(pixels, centerX, centerY + offset, color);
            }
        }

        private static void DrawNumber(Color32[] pixels, int number, int left, int top, Color32 color)
        {
            string text = number.ToString(CultureInfo.InvariantCulture);
            int cursorX = left;
            for (int i = 0; i < text.Length; i++)
            {
                int digit = text[i] - '0';
                DrawDigit(pixels, digit, cursorX, top, color);
                cursorX += (DigitWidth + DigitSpacing) * DigitScale;
            }
        }

        private static void DrawDigit(Color32[] pixels, int digit, int left, int top, Color32 color)
        {
            for (int row = 0; row < DigitHeight; row++)
            {
                int rowBits = DigitRows[digit, row];
                for (int column = 0; column < DigitWidth; column++)
                {
                    int bit = 1 << (DigitWidth - column - 1);
                    if ((rowBits & bit) == 0)
                    {
                        continue;
                    }

                    for (int scaleY = 0; scaleY < DigitScale; scaleY++)
                    {
                        for (int scaleX = 0; scaleX < DigitScale; scaleX++)
                        {
                            int x = left + column * DigitScale + scaleX;
                            int y = top - row * DigitScale - scaleY;
                            SetPixel(pixels, x, y, color);
                        }
                    }
                }
            }
        }

        private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x < 0 || x >= CellWidth || y < 0 || y >= CellHeight)
            {
                return;
            }

            pixels[y * CellWidth + x] = color;
        }

        private static void CopyCellToSheet(
            Color32[] cellPixels,
            Color32[] sheetPixels,
            int sampleIndex,
            int rowCount,
            int sheetWidth)
        {
            int column = sampleIndex % _columns;
            int rowFromTop = sampleIndex / _columns;
            int destinationX = column * CellWidth;
            int destinationY = (rowCount - rowFromTop - 1) * CellHeight;
            for (int y = 0; y < CellHeight; y++)
            {
                int sourceIndex = y * CellWidth;
                int destinationIndex = (destinationY + y) * sheetWidth + destinationX;
                Array.Copy(cellPixels, sourceIndex, sheetPixels, destinationIndex, CellWidth);
            }
        }

        private static void WriteCsv()
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine(
                "sample,normalizedTime,clipFrame,effectorViewportX,effectorViewportY,effectorDepth,"
                + "effectorSpeed,leftElbowAngle,rightElbowAngle,leftKneeAngle,rightKneeAngle,"
                + "rightHandBodyX,rightHandBodyY,rightHandBodyZ,leftHandBodyX,leftHandBodyY,"
                + "leftHandBodyZ,leftFootHeight,rightFootHeight");

            for (int i = 0; i < Samples.Count; i++)
            {
                AppendCsvSample(csv, Samples[i]);
            }

            File.WriteAllText(_csvPath, csv.ToString(), new UTF8Encoding(false));
        }

        private static void AppendCsvSample(StringBuilder csv, Sample sample)
        {
            csv.Append(sample.Number.ToString(CultureInfo.InvariantCulture));
            AppendCsvFloat(csv, sample.NormalizedTime);
            csv.Append(',').Append(sample.ClipFrame.ToString(CultureInfo.InvariantCulture));
            AppendCsvFloat(csv, sample.ViewportPosition.x);
            AppendCsvFloat(csv, sample.ViewportPosition.y);
            AppendCsvFloat(csv, sample.ViewportPosition.z);
            AppendCsvFloat(csv, sample.MovementSpeed);
            AppendCsvFloat(csv, sample.LeftElbowAngle);
            AppendCsvFloat(csv, sample.RightElbowAngle);
            AppendCsvFloat(csv, sample.LeftKneeAngle);
            AppendCsvFloat(csv, sample.RightKneeAngle);
            AppendCsvFloat(csv, sample.RightHandBodyPosition.x);
            AppendCsvFloat(csv, sample.RightHandBodyPosition.y);
            AppendCsvFloat(csv, sample.RightHandBodyPosition.z);
            AppendCsvFloat(csv, sample.LeftHandBodyPosition.x);
            AppendCsvFloat(csv, sample.LeftHandBodyPosition.y);
            AppendCsvFloat(csv, sample.LeftHandBodyPosition.z);
            AppendCsvFloat(csv, sample.LeftFootHeight);
            AppendCsvFloat(csv, sample.RightFootHeight);
            csv.AppendLine();
        }

        private static void AppendCsvFloat(StringBuilder csv, float number)
        {
            csv.Append(',').Append(number.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void StopCapture()
        {
            EditorApplication.update -= OnEditorUpdate;
            if (!_isCaptureActive)
            {
                return;
            }

            Time.timeScale = _savedTimeScale;
            _isCaptureActive = false;
            _animator = null;
            _camera = null;
            _effector = null;
            CellPixels.Clear();
            Samples.Clear();
        }

        private struct Sample
        {
            public int Number;
            public float NormalizedTime;
            public int ClipFrame;
            public Vector3 EffectorWorldPosition;
            public Vector3 ViewportPosition;
            public float MovementSpeed;
            public float LeftElbowAngle;
            public float RightElbowAngle;
            public float LeftKneeAngle;
            public float RightKneeAngle;
            public Vector3 RightHandBodyPosition;
            public Vector3 LeftHandBodyPosition;
            public float LeftFootHeight;
            public float RightFootHeight;
        }
    }
}

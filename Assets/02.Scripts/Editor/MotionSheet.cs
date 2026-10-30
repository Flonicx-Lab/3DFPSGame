using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FpsGame.Editor
{
    // 프레임마다 게임 시간을 고정해 애니메이션 상태의 화면과 자세 수치를 기록한다.
    public static class MotionSheet
    {
        private const int CellWidth = 480;
        private const int CellHeight = 270;
        private const int DigitWidth = 3;
        private const int DigitHeight = 5;
        private const int DigitScale = 2;
        private const int DigitSpacing = 1;
        private const float StateStartTimeout = 2f;
        private const float CaptureTimeout = 120f;
        private const float LastSampleThreshold = 0.99f;

        private static readonly Color32 s_trajectoryColor = new(255, 220, 0, 255);
        private static readonly Color32 s_currentPointColor = new(255, 0, 0, 255);
        private static readonly Color32 s_centerColor = new(0, 255, 0, 255);
        private static readonly Color32 s_numberColor = new(255, 255, 255, 255);
        private static readonly Color32 s_emptyCellColor = new(0, 0, 0, 255);
        private static readonly int[,] s_digitRows =
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

        private static readonly List<Color32[]> s_cellPixels = new();
        private static readonly List<Sample> s_samples = new();

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
        private static float _savedCaptureDeltaTime;
        private static float _savedTimeScale;
        private static int _sampleIndex;
        private static float _clipLength;
        private static float _clipFrameRate;
        private static double _captureStartedAt;
        private static int _lastObservedFrame;
        private static bool _stateStarted;
        private static bool _hasLastFrameSnapshot;
        private static bool _isCaptureDeltaTimeConfigured;
        private static bool _useStateLengthFallback;
        private static Snapshot _lastFrameSnapshot;
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
                _savedCaptureDeltaTime = Time.captureDeltaTime;
                _savedTimeScale = Time.timeScale;
                _isCaptureActive = true;
                Time.timeScale = 1f;
                EditorApplication.update += OnEditorUpdate;
                _animator.Play(_stateName, _layer, 0f);

                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(_layer);
                if (stateInfo.IsName(_stateName))
                {
                    ConfigureCaptureDeltaTime(stateInfo);
                }
                else
                {
                    _useStateLengthFallback = true;
                }
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
            _captureStartedAt = EditorApplication.timeSinceStartup;
            _lastObservedFrame = -1;
            _stateStarted = false;
            _hasLastFrameSnapshot = false;
            _isCaptureDeltaTimeConfigured = false;
            _useStateLengthFallback = false;
            s_cellPixels.Clear();
            s_samples.Clear();
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

        private static void OnEditorUpdate()
        {
            if (!_isCaptureActive)
            {
                return;
            }

            try
            {
                var elapsedTime = EditorApplication.timeSinceStartup - _captureStartedAt;
                if (elapsedTime > CaptureTimeout)
                {
                    FailCapture($"MotionSheet 캡처가 실시간 {CaptureTimeout}초를 초과했습니다.");
                    return;
                }

                AnimatorStateInfo stateInfo = GetObservedStateInfo();
                var isTargetState = stateInfo.IsName(_stateName);
                if (!_stateStarted)
                {
                    if (!isTargetState)
                    {
                        if (elapsedTime > StateStartTimeout)
                        {
                            FailCapture($"애니메이션 상태가 실시간 {StateStartTimeout}초 안에 시작되지 않았습니다: {_stateName}");
                        }

                        return;
                    }

                    _stateStarted = true;
                }
                else if (!isTargetState)
                {
                    CaptureLastSampleBeforeStateExit();
                    return;
                }

                if (!_isCaptureDeltaTimeConfigured)
                {
                    ConfigureCaptureDeltaTimeAfterPlay();
                    if (!_isCaptureDeltaTimeConfigured)
                    {
                        return;
                    }
                }

                if (_lastObservedFrame == Time.frameCount)
                {
                    return;
                }

                _lastObservedFrame = Time.frameCount;
                CaptureReachedSamples(Mathf.Clamp01(stateInfo.normalizedTime));
            }
            catch (Exception exception)
            {
                Debug.LogError($"MotionSheet 캡처에 실패했습니다.\n{exception}");
                StopCapture();
            }
        }

        private static AnimatorStateInfo GetObservedStateInfo()
        {
            return _animator.IsInTransition(_layer)
                ? _animator.GetNextAnimatorStateInfo(_layer)
                : _animator.GetCurrentAnimatorStateInfo(_layer);
        }

        private static void CaptureReachedSamples(float normalizedTime)
        {
            Snapshot snapshot = default;
            var hasSnapshot = false;
            var capturedSampleCount = 0;

            while (_sampleIndex < _sampleCount - 1)
            {
                var targetTime = _sampleIndex / (float)(_sampleCount - 1);
                if (normalizedTime < targetTime)
                {
                    break;
                }

                if (!hasSnapshot)
                {
                    snapshot = CaptureSnapshot(normalizedTime);
                    hasSnapshot = true;
                }

                AddSnapshot(snapshot, _sampleIndex);
                _sampleIndex++;
                capturedSampleCount++;
            }

            if (_sampleIndex != _sampleCount - 1)
            {
                LogOverlappingSamples(capturedSampleCount);
                return;
            }

            if (!hasSnapshot)
            {
                snapshot = CaptureSnapshot(normalizedTime);
            }

            if (normalizedTime >= LastSampleThreshold)
            {
                AddSnapshot(snapshot, _sampleIndex);
                _sampleIndex++;
                capturedSampleCount++;
                LogOverlappingSamples(capturedSampleCount);
                CompleteCapture();
                return;
            }

            LogOverlappingSamples(capturedSampleCount);
            _lastFrameSnapshot = snapshot;
            _hasLastFrameSnapshot = true;
        }

        private static void CaptureLastSampleBeforeStateExit()
        {
            if (_sampleIndex != _sampleCount - 1 || !_hasLastFrameSnapshot)
            {
                FailCapture($"애니메이션 상태가 모든 샘플 시점에 도달하기 전에 종료되었습니다: {_stateName}");
                return;
            }

            AddSnapshot(_lastFrameSnapshot, _sampleIndex);
            _sampleIndex++;
            CompleteCapture();
        }

        private static Snapshot CaptureSnapshot(float normalizedTime)
        {
            if (_sampleIndex == 0)
            {
                CacheClipTiming();
            }

            Vector3 effectorWorldPosition = _effector.TransformPoint(_effectorLocalPoint);
            Vector3 viewportPosition = _camera.WorldToViewportPoint(effectorWorldPosition);
            var movementSpeed = CalculateMovementSpeed(effectorWorldPosition);
            Vector3 rightAxis = (_rightShoulder.position - _leftShoulder.position).normalized;
            Vector3 upAxis = Vector3.ProjectOnPlane(_neck.position - _hips.position, rightAxis).normalized;
            Vector3 forwardAxis = Vector3.Cross(rightAxis, upAxis).normalized;

            var sample = new Sample
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

            return new Snapshot
            {
                Sample = sample,
                Pixels = RenderCell(),
            };
        }

        private static void AddSnapshot(Snapshot snapshot, int sampleIndex)
        {
            Sample sample = snapshot.Sample;
            sample.Number = sampleIndex + 1;
            s_samples.Add(sample);
            s_cellPixels.Add((Color32[])snapshot.Pixels.Clone());
        }

        private static void LogOverlappingSamples(int capturedSampleCount)
        {
            if (capturedSampleCount > 1)
            {
                Debug.LogWarning($"MotionSheet 샘플 목표 {capturedSampleCount}개가 같은 프레임에 겹쳤습니다.");
            }
        }

        private static void CompleteCapture()
        {
            WriteOutputs();
            var pngPath = _pngPath;
            var csvPath = _csvPath;
            StopCapture();
            Debug.Log($"MotionSheet PNG 저장 완료: {pngPath}\nMotionSheet CSV 저장 완료: {csvPath}");
        }

        private static void FailCapture(string message)
        {
            Debug.LogError(message);
            StopCapture();
        }

        private static void CacheClipTiming()
        {
            AnimatorClipInfo[] clipInfos = _animator.IsInTransition(_layer)
                && _animator.GetNextAnimatorStateInfo(_layer).IsName(_stateName)
                    ? _animator.GetNextAnimatorClipInfo(_layer)
                    : _animator.GetCurrentAnimatorClipInfo(_layer);
            if (clipInfos.Length == 0)
            {
                throw new InvalidOperationException("지정한 상태에서 재생 중인 AnimationClip을 찾을 수 없습니다.");
            }

            AnimatorClipInfo selectedClipInfo = clipInfos[0];
            for (var i = 1; i < clipInfos.Length; i++)
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

        private static void ConfigureCaptureDeltaTimeAfterPlay()
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(_layer);
            if (!stateInfo.IsName(_stateName))
            {
                return;
            }

            if (_useStateLengthFallback)
            {
                SetCaptureDeltaTime(stateInfo.length);
                return;
            }

            ConfigureCaptureDeltaTime(stateInfo);
        }

        private static void ConfigureCaptureDeltaTime(AnimatorStateInfo stateInfo)
        {
            CacheClipTiming();
            var playbackSpeed = Mathf.Abs(stateInfo.speed * stateInfo.speedMultiplier);
            if (Mathf.Approximately(playbackSpeed, 0f))
            {
                throw new InvalidOperationException("애니메이션 상태의 실제 재생 속도는 0보다 커야 합니다.");
            }

            SetCaptureDeltaTime(_clipLength / playbackSpeed);
        }

        private static void SetCaptureDeltaTime(float playbackLength)
        {
            if (playbackLength <= 0f)
            {
                throw new InvalidOperationException("애니메이션 상태의 실제 재생 길이는 0보다 커야 합니다.");
            }

            Time.captureDeltaTime = playbackLength / (_sampleCount * 4f);
            _isCaptureDeltaTimeConfigured = true;
        }

        private static float CalculateMovementSpeed(Vector3 effectorWorldPosition)
        {
            if (s_samples.Count == 0)
            {
                return 0f;
            }

            var sampleDuration = _clipLength / (_sampleCount - 1);
            Vector3 previousPosition = s_samples[s_samples.Count - 1].EffectorWorldPosition;
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
            var renderTexture = RenderTexture.GetTemporary(CellWidth, CellHeight, 24);
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
            var directoryPath = Path.GetDirectoryName(_pngPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            WritePng();
            WriteCsv();
        }

        private static void WritePng()
        {
            var rowCount = Mathf.CeilToInt(_sampleCount / (float)_columns);
            var sheetWidth = _columns * CellWidth;
            var sheetHeight = rowCount * CellHeight;
            var sheetPixels = new Color32[sheetWidth * sheetHeight];
            for (var i = 0; i < sheetPixels.Length; i++)
            {
                sheetPixels[i] = s_emptyCellColor;
            }

            for (var i = 0; i < s_cellPixels.Count; i++)
            {
                DrawCellOverlay(s_cellPixels[i], i);
                CopyCellToSheet(s_cellPixels[i], sheetPixels, i, rowCount, sheetWidth);
            }

            var sheetTexture = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
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
            for (var i = 1; i < s_samples.Count; i++)
            {
                Vector3 startViewport = s_samples[i - 1].ViewportPosition;
                Vector3 endViewport = s_samples[i].ViewportPosition;
                if (startViewport.z <= 0f || endViewport.z <= 0f)
                {
                    continue;
                }

                DrawViewportLine(pixels, startViewport, endViewport, s_trajectoryColor);
            }

            Vector3 currentViewport = s_samples[currentSampleIndex].ViewportPosition;
            DrawCross(pixels, CellWidth / 2, CellHeight / 2, 4, s_centerColor);
            if (currentViewport.z > 0f)
            {
                var currentX = Mathf.RoundToInt(currentViewport.x * (CellWidth - 1));
                var currentY = Mathf.RoundToInt(currentViewport.y * (CellHeight - 1));
                DrawCircle(pixels, currentX, currentY, 4, s_currentPointColor);
            }

            DrawNumber(pixels, s_samples[currentSampleIndex].Number, 6, CellHeight - 6, s_numberColor);
        }

        private static void DrawViewportLine(
            Color32[] pixels,
            Vector3 startViewport,
            Vector3 endViewport,
            Color32 color)
        {
            var startX = startViewport.x * (CellWidth - 1);
            var startY = startViewport.y * (CellHeight - 1);
            var endX = endViewport.x * (CellWidth - 1);
            var endY = endViewport.y * (CellHeight - 1);
            if (!ClipLineToCell(ref startX, ref startY, ref endX, ref endY))
            {
                return;
            }

            var stepCount = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(endX - startX), Mathf.Abs(endY - startY)));
            if (stepCount == 0)
            {
                SetPixel(pixels, Mathf.RoundToInt(startX), Mathf.RoundToInt(startY), color);
                return;
            }

            for (var i = 0; i <= stepCount; i++)
            {
                var ratio = i / (float)stepCount;
                var x = Mathf.RoundToInt(Mathf.Lerp(startX, endX, ratio));
                var y = Mathf.RoundToInt(Mathf.Lerp(startY, endY, ratio));
                SetPixel(pixels, x, y, color);
                SetPixel(pixels, x + 1, y, color);
            }
        }

        private static bool ClipLineToCell(ref float startX, ref float startY, ref float endX, ref float endY)
        {
            var deltaX = endX - startX;
            var deltaY = endY - startY;
            var minimumRatio = 0f;
            var maximumRatio = 1f;

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

            var ratio = distance / direction;
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
            var squaredRadius = radius * radius;
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
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
            for (var offset = -radius; offset <= radius; offset++)
            {
                SetPixel(pixels, centerX + offset, centerY, color);
                SetPixel(pixels, centerX, centerY + offset, color);
            }
        }

        private static void DrawNumber(Color32[] pixels, int number, int left, int top, Color32 color)
        {
            var text = number.ToString(CultureInfo.InvariantCulture);
            var cursorX = left;
            for (var i = 0; i < text.Length; i++)
            {
                var digit = text[i] - '0';
                DrawDigit(pixels, digit, cursorX, top, color);
                cursorX += (DigitWidth + DigitSpacing) * DigitScale;
            }
        }

        private static void DrawDigit(Color32[] pixels, int digit, int left, int top, Color32 color)
        {
            for (var row = 0; row < DigitHeight; row++)
            {
                var rowBits = s_digitRows[digit, row];
                for (var column = 0; column < DigitWidth; column++)
                {
                    var bit = 1 << (DigitWidth - column - 1);
                    if ((rowBits & bit) == 0)
                    {
                        continue;
                    }

                    for (var scaleY = 0; scaleY < DigitScale; scaleY++)
                    {
                        for (var scaleX = 0; scaleX < DigitScale; scaleX++)
                        {
                            var x = left + column * DigitScale + scaleX;
                            var y = top - row * DigitScale - scaleY;
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
            var column = sampleIndex % _columns;
            var rowFromTop = sampleIndex / _columns;
            var destinationX = column * CellWidth;
            var destinationY = (rowCount - rowFromTop - 1) * CellHeight;
            for (var y = 0; y < CellHeight; y++)
            {
                var sourceIndex = y * CellWidth;
                var destinationIndex = (destinationY + y) * sheetWidth + destinationX;
                Array.Copy(cellPixels, sourceIndex, sheetPixels, destinationIndex, CellWidth);
            }
        }

        private static void WriteCsv()
        {
            var csv = new StringBuilder();
            csv.AppendLine(
                "sample,normalizedTime,clipFrame,effectorViewportX,effectorViewportY,effectorDepth,"
                + "effectorSpeed,leftElbowAngle,rightElbowAngle,leftKneeAngle,rightKneeAngle,"
                + "rightHandBodyX,rightHandBodyY,rightHandBodyZ,leftHandBodyX,leftHandBodyY,"
                + "leftHandBodyZ,leftFootHeight,rightFootHeight");

            for (var i = 0; i < s_samples.Count; i++)
            {
                AppendCsvSample(csv, s_samples[i]);
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

            Time.captureDeltaTime = _savedCaptureDeltaTime;
            Time.timeScale = _savedTimeScale;
            _isCaptureActive = false;
            _animator = null;
            _camera = null;
            _effector = null;
            s_cellPixels.Clear();
            s_samples.Clear();
        }

        private struct Snapshot
        {
            public Sample Sample;
            public Color32[] Pixels;
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

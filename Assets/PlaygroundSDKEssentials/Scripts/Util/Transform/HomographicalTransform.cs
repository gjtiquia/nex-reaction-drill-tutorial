using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    /**
     * A tilted (from dewarp) Playground camera creates a perspective
     * raw camera frame, where the vertical axis may appear not vertical
     * as we go further. To make all poses upright, we can mathematically
     * apply a homographical transform
     * [ ref https://en.wikipedia.org/wiki/Homography_(computer_vision) ]
     * so that the poses look upright again.
     */
    public readonly struct HomographicalTransform
    {
        private readonly float ux, uy, uc;
        private readonly float vx, vy, vc;
        private readonly float wx, wy, wc;

        private HomographicalTransform(
            float ux, float uy, float uc,
            float vx, float vy, float vc,
            float wx, float wy, float wc)
        {
            this.ux = ux;
            this.uy = uy;
            this.uc = uc;
            this.vx = vx;
            this.vy = vy;
            this.vc = vc;
            this.wx = wx;
            this.wy = wy;
            this.wc = wc;
        }

        public static HomographicalTransform Compute(float pitch, float fov, float rawFrameWidth, float rawFrameHeight)
        {
            // ReSharper disable InlineTemporaryVariable
            // ReSharper disable ConvertToConstant.Local

            // Conceptually, we need to compute the matrix
            // [ ux uy uc ]
            // [ vx vy vc ]
            // [ wx wy wc ]
            // Mathematically, there should be 8 degrees of freedom for this matrix.
            // However, since we understand the physical meaning of our transform, we can get away with only four
            // parameters.

            // For each point in the raw camera frame, we want to draw a ray from the camera to that point in the real
            // world. The horizontal fov is given by fov. Therefore, half of the raw frame is actually tan(fov / 2)
            // units wide in the physical world, assuming that the raw frame is projected 1 unit away from the camera.
            // Moreover, we are assuming a 1:1 horizontal:vertical pixel size. It follows that the physical frame and
            // the camera frame should have the same aspect ratio.

            // Denote cw/ch as the raw frame width/height, pw as the half physical frame width/height, we have:
            var cw = rawFrameWidth;
            var ch = rawFrameHeight;
            var pw = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
            var k = pw / cw;
            var k2 = 2 * k;
            // Here, k2 is a common scale (2 * pw) / cw == (2 * ph) / ch.

            // Given a point in (x, y), the corresponding point (px, py) in that physical frame is thus:
            // px = (x - cw / 2) * k2, py = (y - ch / 2) * k2
            // Essentially, the ray from camera to (x, y) is (px, py, 1).
            // We want to adjust the camera tilting by pitch, so we want to rotate this ray by pitch.
            // Since this is pitch only, the x coordinate is untouched.
            // However, the y and z coordinates should be rotated accordingly.
            // Mathematically, the direction z should be rotated to (-sin(pitch), cos(pitch))
            // and the y coordinate should be rotated to (cos(pitch), sin(pitch)).
            // Together, we can write
            // u = px
            // v = py * cos(pitch) - sin(pitch)
            // w = py * sin(pitch) + cos(pitch)

            var radPitch = Mathf.Deg2Rad * pitch;
            var cos = Mathf.Cos(radPitch);
            var sin = Mathf.Sin(radPitch);

            float ux = k2, uy = 0, uc = -k * cw;
            float vx = 0, vy = k2 * cos, vc = -k * ch * cos - sin;
            float wx = 0, wy = k2 * sin, wc = -k * ch * sin + cos;

            return new HomographicalTransform(
                ux, uy, uc,
                vx, vy, vc,
                wx, wy, wc
            );

            // ReSharper restore ConvertToConstant.Local
            // ReSharper restore InlineTemporaryVariable
        }

        public Vector2 Transform(Vector2 input)
        {
            var x = input.x;
            var y = input.y;

            var u = ux * x + uy * y + uc;
            var v = vx * x + vy * y + vc;
            var w = wx * x + wy * y + wc;
            return w == 0 ? new Vector2(u, v) : new Vector2(u / w, v / w);
        }

        public void Transform(ref PoseNode node)
        {
            var x = node.x;
            var y = node.y;
            var u = ux * x + uy * y + uc;
            var v = vx * x + vy * y + vc;
            var w = wx * x + wy * y + wc;
            if (w == 0)
            {
                node.x = u;
                node.y = v;
            }
            else
            {
                node.x = u / w;
                node.y = v / w;
            }
        }
    }
}

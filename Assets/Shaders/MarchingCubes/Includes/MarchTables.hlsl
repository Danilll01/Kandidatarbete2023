// Values from http://paulbourke.net/geometry/polygonise/
// The triangulation table lives in a GPU buffer filled from MarchTables.cs, since large constant arrays here made
// the WebGPU shader compilation hang. Only the two small edge-to-corner tables stay in the shader.

static const int cornerIndexAFromEdge[12] =
{
    0,
    1,
    2,
    3,
    4,
    5,
    6,
    7,
    0,
    1,
    2,
    3
};

static const int cornerIndexBFromEdge[12] =
{
    1,
    2,
    3,
    0,
    5,
    6,
    7,
    4,
    4,
    5,
    6,
    7
};
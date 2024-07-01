float2 _LogViewDepthMinDelta;
float WarpDepth(float vd)
{
	//return (log(vd) - _LogViewDepthMinDelta.x) / _LogViewDepthMinDelta.y * 2 - 1;
	//return (log(vd) - _LogViewDepthMinDelta.x) / _LogViewDepthMinDelta.y;
	//return vd;
	//return log(vd) * 2 - 1;
	//return (vd - _LogViewDepthMinDelta.x) / _LogViewDepthMinDelta.y * 2 - 1;

	//return vd * 2 - 1;
	//return (log(vd * 2 - 1) - _LogViewDepthMinDelta.x) / _LogViewDepthMinDelta.y * 2 - 1;

	//return (log(vd) - _LogViewDepthMinDelta.x) / _LogViewDepthMinDelta.y * 2 - 1;
	return saturate((log(vd) - _LogViewDepthMinDelta.x) / _LogViewDepthMinDelta.y) * 2 - 1;
}

// NOTE: vd (z/depth) are computed from linear view-space depth values
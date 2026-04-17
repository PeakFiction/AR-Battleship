using System;

namespace Core.Application.Snapshots;

[Serializable]
public sealed record CellView
{
	public int X { get; init; }
	public int Y { get; init; } 
	public Enums.CellViewState State { get; init; }

	public CellView(int x, int y, Enums.CellViewState state)
	{
		X = x;
		Y = y;
		State = state;
	}
}

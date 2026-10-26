namespace BenildeTrack.Models;

public enum AttendanceType { Absent, Late }

// Objectives and clearances work the same way, so they share one table and this tells them apart.
public enum RequirementKind { Objective, Clearance }

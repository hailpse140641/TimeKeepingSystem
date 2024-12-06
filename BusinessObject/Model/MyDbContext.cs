﻿using BusinessObject.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using System.Runtime.ConstrainedExecution;
using System.Text.Json;

public class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }
    public MyDbContext(DbContextOptions<MyDbContext> options, IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    //private String URL = "Server=DESKTOP-14337NG;Database=TimeSystem;User id=sa;Password=root;TrustServerCertificate=true;";
    private String URL = "Data Source=SQL9001.site4now.net;Initial Catalog=db_aaf2fb_timesystem;User Id=db_aaf2fb_timesystem_admin;Password=guma0504;";

    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RiskPerformanceEmployee> RiskPerformanceEmployees { get; set; }
    public DbSet<Holiday> DepartmentHolidays { get; set; }
    public DbSet<DepartmentHolidayException> DepartmentHolidayExceptions { get; set; }
    public DbSet<AttendanceStatus> AttendanceStatuses { get; set; }
    public DbSet<Team> Departments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<RequestLeave> RequestLeaves { get; set; }
    public DbSet<RequestOverTime> RequestOverTimes { get; set; }
    public DbSet<RequestWorkTime> RequestWorkTimes { get; set; }
    public DbSet<RiskPerformanceSetting> RiskPerformanceSettings { get; set; }
    public DbSet<WorkDateSetting> WorkDateSettings { get; set; }
    public DbSet<WorkingStatus> WorkingStatuses { get; set; }
    public DbSet<WorkPermissionSetting> WorkPermissionSettings { get; set; }
    public DbSet<Workslot> Workslots { get; set; }
    public DbSet<WorkslotEmployee> WorkslotEmployees { get; set; }
    public DbSet<WorkTimeSetting> WorkTimeSettings { get; set; }
    public DbSet<WorkTrackSetting> WorkTrackSettings { get; set; }
    public DbSet<LeaveSetting> LeaveSettings { get; set; }

    public DbSet<Wifi> Wifis { get; set; }
    private readonly IConfiguration _configuration;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (!optionsBuilder.IsConfigured)
        {
            //optionsBuilder.UseSqlServer("Server=DESKTOP-14337NG;Database=TimeBackup;User id=sa;Password=root;TrustServerCertificate=true;");
            //optionsBuilder.UseSqlServer("Server=tcp:time-keeping.database.windows.net,1433;Initial Catalog=TimeSystem;Persist Security Info=False;User ID=guma1234;Password=doan1234!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=100;");
            //optionsBuilder.UseSqlServer(_configuration.GetConnectionString("online"));
            optionsBuilder.UseSqlServer(URL);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Request>()
        .HasOne(r => r.RequestLeave)
        .WithMany()
        .HasForeignKey(r => r.RequestLeaveId)
        .OnDelete(DeleteBehavior.Restrict);

        // Request to RequestWorkTime relationship
        modelBuilder.Entity<Request>()
            .HasOne(r => r.RequestWorkTime)
            .WithMany()
            .HasForeignKey(r => r.RequestWorkTimeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Request to Employee relationship
        modelBuilder.Entity<Request>()
            .HasOne(r => r.EmployeeSendRequest)
            .WithMany()
            .HasForeignKey(r => r.EmployeeSendRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        // Request to RequestOverTime relationship
        modelBuilder.Entity<Request>()
            .HasOne(r => r.RequestOverTime)
            .WithMany()
            .HasForeignKey(r => r.RequestOverTimeId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkslotEmployee to Employee relationship
        modelBuilder.Entity<WorkslotEmployee>()
            .HasOne(we => we.Employee)
            .WithMany()
            .HasForeignKey(we => we.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkslotEmployee to Workslot relationship
        modelBuilder.Entity<WorkslotEmployee>()
            .HasOne(we => we.Workslot)
            .WithMany()
            .HasForeignKey(we => we.WorkslotId)
            .OnDelete(DeleteBehavior.Restrict);

        // Employee to Manager (self-reference) relationship
        //modelBuilder.Entity<Employee>()
        //    .HasOne(e => e.Manager)
        //    .WithMany()
        //    .HasForeignKey(e => e.ManagerId)
        //    .OnDelete(DeleteBehavior.Restrict);

        // Configure Team to Employee relationship
        modelBuilder.Entity<Team>()
            .HasMany(d => d.Employees)  // Assuming Team has a collection property named Employees
            .WithOne(e => e.Department)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
        .HasOne(e => e.UserAccount)
        .WithOne(ua => ua.Employee)
        .HasForeignKey<UserAccount>(ua => ua.EmployeeId);

        // Fluent API to configure EmploymentType column
        modelBuilder.Entity<Employee>()
            .Property(e => e.EmploymentType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("FullTime");

        modelBuilder.Entity<WorkTrackSetting>()
            .Property(wts => wts.MaxDateLeaves)
            .HasDefaultValue("[]")
            .IsRequired();

        modelBuilder.Entity<Holiday>().ToTable("DepartmentHolidays");

        modelBuilder.Entity<AttendanceStatus>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Team>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Holiday>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DepartmentHolidayException>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LeaveSetting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LeaveType>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Request>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RequestLeave>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RequestOverTime>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RequestWorkTime>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RiskPerformanceEmployee>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RiskPerformanceSetting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserAccount>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Wifi>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkDateSetting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkingStatus>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkPermissionSetting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Workslot>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkslotEmployee>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkTimeSetting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkTrackSetting>().HasQueryFilter(e => !e.IsDeleted);
    }
}
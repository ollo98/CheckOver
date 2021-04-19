﻿using CheckOver.Data;
using CheckOver.Models;
using CheckOver.Models.ViewModels;
using CheckOver.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckOver.Repository
{
    public class ExerciseRepository : IExerciseRepository
    {
        private readonly ApplicationDbContext context;
        private readonly IUserService userService;
        private readonly IGroupRepository groupRepository;

        public ExerciseRepository(ApplicationDbContext context, IUserService userService, IGroupRepository groupRepository)
        {
            this.context = context;
            this.userService = userService;
            this.groupRepository = groupRepository;
        }

        public async Task<int> AddExercise(MakeExerciseVM makeExerciseVM)
        {
            var userId = userService.GetUserId();
            var User = context.Users.FirstOrDefault(x => x.Id == userId);
            var newExercise = new Exercise
            {
                Title = makeExerciseVM.Title,
                Description = makeExerciseVM.Description,
                CreatedAt = DateTime.Now,
                MaxPoints = makeExerciseVM.MaxPoints,
                Creator = User,
                CreatorId = userId,
                DeadLine = DateTime.ParseExact(makeExerciseVM.DeadLineString, "dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture)
            };
            await context.Exercises.AddAsync(newExercise);
            await context.SaveChangesAsync();
            return newExercise.ExerciseId;
        }

        public async Task<List<Exercise>> GetUserExercises()
        {
            var userId = userService.GetUserId();
            var User = context.Users.FirstOrDefault(x => x.Id == userId);
            var exercises = await context.Exercises
                .Include(x => x.Creator).Where(x => x.CreatorId == userId)
                .ToListAsync();
            return exercises;
        }

        public async Task AssignExerciseToUsers(int GroupId, int ExerciseId)
        {
            var assignments = await groupRepository.getMembers(GroupId);
            var exercise = await context.Exercises.FirstOrDefaultAsync(x => x.ExerciseId == ExerciseId);
            foreach (var item in assignments)
            {
                var newSolving = new Solving()
                {
                    AssignmentId = item.AssignmentId,
                    Status = "Do wykonania",
                    CreatedAt = null,
                    ProgrammingLanguage = "Not now",
                    Exercise = exercise,
                    ExerciseId = ExerciseId
                };
                await context.Solvings.AddAsync(newSolving);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Solving>> GetUserSolvings()
        {
            var userId = userService.GetUserId();
            var User = context.Users.FirstOrDefault(x => x.Id == userId);
            var userAssignments = await context.Assignments
                .Where(x => x.UserId == userId).ToListAsync();
            List<Solving> solvings = new List<Solving>();
            foreach (var item in userAssignments)
            {
                solvings.AddRange(context.Solvings
                    .Include(x => x.Exercise)
                    .Include(x => x.Checking)
                    .ThenInclude(x => x.Checker)
                    .Include(x => x.Assignment)
                    .ThenInclude(x => x.Group)
                    .Where(x => x.AssignmentId == item.AssignmentId));
            }
            return solvings;
        }

        public async Task<Solving> GetSolvingById(int SolvingId)
        {
            return await context.Solvings
                .Include(x => x.Checking)
                .Include(x => x.Exercise)
                .Include(x => x.Assignment)
                .ThenInclude(x => x.Group)
                .FirstOrDefaultAsync(x => x.SolvingId == SolvingId);
        }

        public async Task ReceiveSolvedExercise(SolvedExerciseVM solvedExerciseVM, int solvingId)
        {
            var solving = await context.Solvings.FirstOrDefaultAsync(x => x.SolvingId == solvingId);
            solving.Status = "Do sprawdzenia";
            solving.CreatedAt = DateTime.Now;
            solving.Answer = solvedExerciseVM.Answer;
            await context.SaveChangesAsync();
        }

        public async Task<List<Solving>> ShowExercisesToCheck()
        {
            var userId = userService.GetUserId();
            var User = context.Users.FirstOrDefault(x => x.Id == userId);
            var solvings = await context.Assignments
                .Include(x => x.Group)
                .ThenInclude(x => x.Assignments)
                .Include(x => x.Role)
                .Where(x => x.UserId == userId)
                .Select(x => x.Group)
                .SelectMany(x => x.Assignments)
                .Where(x => x.Role.Name == "Creator" && x.UserId != userId)
                .Include(x => x.Solvings)
                .SelectMany(x => x.Solvings)
                .Where(x => x.Status == "Do sprawdzenia")
                .Include(x => x.Checking)
                .Include(x => x.Assignment)
                .ThenInclude(x => x.Group)
                .Include(x => x.Assignment)
                .ThenInclude(x => x.User)
                .Include(x => x.Exercise)
                .ToListAsync();

            return solvings;
        }

        public async Task ProcessCheckedExercise(CheckTheExerciseVM checkTheExerciseVM, int SolvingId)
        {
            var userId = userService.GetUserId();
            var User = context.Users.FirstOrDefault(x => x.Id == userId);
            var solving = await GetSolvingById(SolvingId);
            solving.Status = "Sprawdzone";
            var newChecking = new Checking()
            {
                Solving = solving,
                SolvingId = SolvingId,
                Points = checkTheExerciseVM.Points,
                Remarks = checkTheExerciseVM.Remarks,
                Checker = User,
                CheckerId = userId,
                CreatedAt = DateTime.Now
            };
            await context.Checkings.AddAsync(newChecking);
            await context.SaveChangesAsync();
        }

        public async Task<List<Solving>> ShowCheckedExercises()
        {
            var userId = userService.GetUserId();
            var User = context.Users.FirstOrDefault(x => x.Id == userId);
            var solvings = await context.Solvings
                .Include(x => x.Checking)
                .ThenInclude(x => x.Checker)
                .Include(x => x.Assignment)
                .ThenInclude(x => x.Group)
                .Include(x => x.Exercise)
                .Where(x => x.Status == "Sprawdzone" && x.Assignment.UserId == userId)
                .ToListAsync();
            return solvings;
        }

        public int function()
        {
            return 0;
        }
    }
}
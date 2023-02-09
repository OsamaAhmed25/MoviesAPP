using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApp.Models;
using NToastNotify;
using MoviesApp.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;                                                          
using System.Linq;
using System.Threading.Tasks;

namespace MoviesApp.Controllers
{
    public class MoviesController : Controller
    {
        private readonly MovieDbContext _context;
        private readonly IToastNotification _toastNotification;
        private List<string> _allowedExtentions = new List<string> { ".jpg", ".png" };
        private long _maxAllowedPoster = 1048576;
        public MoviesController ( MovieDbContext context, IToastNotification toastNotification )
        {
            _context = context;
            this._toastNotification = toastNotification;
        }
        public async Task<IActionResult> Index ( )
        {
            var movies = await _context.Movies.OrderByDescending ( m => m.Rate ).ToListAsync ( );
            return View ( movies );
        }
        public async Task<IActionResult> Create ( )
        {
            var viewModel = new MovieFormVM
            {
                Genres = await _context.Genres.OrderBy ( a => a.Name ).ToListAsync ( )
            };
            return View ( "MovieForm", viewModel );
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create ( MovieFormVM model )
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy ( a => a.Name ).ToListAsync ( );
                return View ( "MovieForm", model );
            }

            var files = Request.Form.Files;
            if (!files.Any ( ))
            {
                model.Genres = await _context.Genres.OrderBy ( a => a.Name ).ToListAsync ( );
                ModelState.AddModelError ( "Poster", "Please Select Movie Poster!" );
                return View ( "MovieForm", model );

            }

            var poster = files.FirstOrDefault ( );

            if (!_allowedExtentions.Contains ( Path.GetExtension ( poster.FileName ).ToLower ( ) ))
            {
                model.Genres = await _context.Genres.OrderBy ( a => a.Name ).ToListAsync ( );
                ModelState.AddModelError ( "Poster", "Only .jpg and .png images are allowed!" );
                return View ( "MovieForm", model );
            }

            if (poster.Length > _maxAllowedPoster)
            {
                model.Genres = await _context.Genres.OrderBy ( a => a.Name ).ToListAsync ( );
                ModelState.AddModelError ( "Poster", "Poster Cannot be More than 1 MB !" );
                return View ( "MovieForm", model );
            }

            using var dataStream = new MemoryStream ( );
            await poster.CopyToAsync ( dataStream );

            var movies = new Movie ( )
            {
                Title = model.Title,
                GenreId = model.GenreId,
                Year = model.Year,
                Rate = model.Rate,
                StoreLine = model.StoreLine,
                Poster = dataStream.ToArray ( )
            };

            _context.Movies.Add ( movies );
            _context.SaveChanges ( );

            _toastNotification.AddSuccessToastMessage ( "Movie Created Successfully" );

            return RedirectToAction ( "Index" );
        }
        public async Task<IActionResult> Edit ( int? id )
        {
            if (id == null)
                return BadRequest ( );
            var movie = await _context.Movies.FindAsync ( id );
            if (movie == null)
                return NotFound ( );
            var viewModel = new MovieFormVM
            {
                id = movie.Id,
                Title = movie.Title,
                GenreId = movie.GenreId,
                Rate = movie.Rate,
                Year = movie.Year,
                StoreLine = movie.StoreLine,
                Poster = movie.Poster,
                Genres = await _context.Genres.OrderBy ( m => m.Name ).ToListAsync ( )

            };
            return View ( "MovieForm", viewModel );

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit ( MovieFormVM model )
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy ( a => a.Name ).ToListAsync ( );
                return View ( "MovieForm", model );
            }

            var movie = await _context.Movies.FindAsync ( model.id );
            if (movie == null)
                return NotFound ( );

            var files = Request.Form.Files;
            if (files.Any ( ))
            {
                var poster = files.FirstOrDefault ( );
                using var datastream = new MemoryStream ( );
                await poster.CopyToAsync ( datastream );
                model.Poster = datastream.ToArray ( );
                if (!_allowedExtentions.Contains ( Path.GetExtension ( poster.FileName ).ToLower ( ) ))
                {
                    model.Genres = await _context.Genres.OrderBy ( a => a.Name ).ToListAsync ( );
                    ModelState.AddModelError ( "Poster", "Only .jpg and .png images are allowed!" );
                    return View ( "MovieForm", model );
                }

                if (poster.Length > _maxAllowedPoster)
                {
                    model.Genres = await _context.Genres.OrderBy ( a => a.Name ).ToListAsync ( );
                    ModelState.AddModelError ( "Poster", "Poster Cannot be More than 1 MB !" );
                    return View ( "MovieForm", model );
                }
                movie.Poster = model.Poster;
            }

            movie.Title = model.Title;
            movie.GenreId = model.GenreId;
            movie.Rate = model.Rate;
            movie.Year = model.Year;
            movie.StoreLine = model.StoreLine;

            _context.SaveChanges ( );

            _toastNotification.AddSuccessToastMessage ( "Movie Edited Successfully" );
            return RedirectToAction ( nameof ( Index ) );

        }
        public async Task<IActionResult> Details ( int? id )
        {
            if (id == null)
                return BadRequest ( );
            var movie = await _context.Movies.Include(m=>m.Genre).SingleOrDefaultAsync(m => m.Id == id);
            if (movie == null)
                return NotFound ( );
            return View ( movie );

        }
        public async Task<IActionResult> Delete(int? id)
        {
            if(id == null)
                return BadRequest();
            var movie = await _context.Movies.FindAsync(id);
            if(movie == null)
                return NotFound();
            _context.Movies.Remove(movie);
            _context.SaveChanges();
            return Ok();

        }
    }
}

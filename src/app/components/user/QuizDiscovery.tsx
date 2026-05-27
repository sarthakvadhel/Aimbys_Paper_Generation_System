import { useState } from 'react';
import { Search, Filter, Clock, Users, Star, BookOpen, Play, ChevronDown } from 'lucide-react';
import { motion } from 'motion/react';
import { AppState } from '../../App';

const allQuizzes = [
  { id: 1, title: 'React Fundamentals', category: 'React', questions: 25, duration: 20, difficulty: 'Medium', participants: 1240, rating: 4.8, description: 'Core React concepts: hooks, state, props, and component lifecycle.', tags: ['hooks', 'state', 'jsx'], emoji: '⚛️', new: true, live: true },
  { id: 2, title: 'Advanced JavaScript Patterns', category: 'JavaScript', questions: 40, duration: 30, difficulty: 'Hard', participants: 980, rating: 4.7, description: 'Closures, prototypes, async/await, and design patterns in JS.', tags: ['closures', 'async', 'ES6'], emoji: '🟨', new: false, live: false },
  { id: 3, title: 'Python Data Structures', category: 'Python', questions: 35, duration: 25, difficulty: 'Medium', participants: 2100, rating: 4.9, description: 'Lists, dicts, sets, tuples, and algorithmic thinking in Python.', tags: ['lists', 'dicts', 'algorithms'], emoji: '🐍', new: false, live: false },
  { id: 4, title: 'CSS Grid & Flexbox', category: 'CSS', questions: 20, duration: 15, difficulty: 'Easy', participants: 1580, rating: 4.9, description: 'Master responsive layouts with modern CSS techniques.', tags: ['grid', 'flexbox', 'responsive'], emoji: '🎨', new: false, live: false },
  { id: 5, title: 'System Design Primer', category: 'System Design', questions: 15, duration: 35, difficulty: 'Hard', participants: 540, rating: 4.6, description: 'Scalability, load balancing, caching, and distributed systems.', tags: ['scalability', 'caching', 'microservices'], emoji: '🏗️', new: true, live: false },
  { id: 6, title: 'SQL Query Optimization', category: 'Databases', questions: 30, duration: 25, difficulty: 'Medium', participants: 890, rating: 4.7, description: 'Indexes, joins, query plans, and performance tuning.', tags: ['indexes', 'joins', 'performance'], emoji: '🗄️', new: false, live: false },
  { id: 7, title: 'Algorithm Complexity', category: 'Algorithms', questions: 45, duration: 40, difficulty: 'Hard', participants: 770, rating: 4.5, description: 'Big-O notation, sorting algorithms, and dynamic programming.', tags: ['big-o', 'sorting', 'dp'], emoji: '🧮', new: false, live: true },
  { id: 8, title: 'Docker Basics', category: 'DevOps', questions: 25, duration: 20, difficulty: 'Easy', participants: 450, rating: 4.4, description: 'Containers, images, volumes, and Docker Compose.', tags: ['containers', 'images', 'compose'], emoji: '🐳', new: true, live: false },
  { id: 9, title: 'TypeScript Advanced', category: 'TypeScript', questions: 30, duration: 25, difficulty: 'Hard', participants: 620, rating: 4.6, description: 'Generics, utility types, conditional types, and decorators.', tags: ['generics', 'types', 'inference'], emoji: '🔷', new: false, live: false },
  { id: 10, title: 'REST API Design', category: 'Backend', questions: 28, duration: 22, difficulty: 'Medium', participants: 720, rating: 4.7, description: 'RESTful principles, status codes, auth, and API versioning.', tags: ['REST', 'auth', 'versioning'], emoji: '🔌', new: false, live: false },
  { id: 11, title: 'React Native Basics', category: 'React', questions: 22, duration: 18, difficulty: 'Easy', participants: 830, rating: 4.5, description: 'Mobile development with React Native: navigation, components.', tags: ['mobile', 'navigation', 'RN'], emoji: '📱', new: false, live: false },
  { id: 12, title: 'Kubernetes Essentials', category: 'DevOps', questions: 32, duration: 30, difficulty: 'Hard', participants: 390, rating: 4.5, description: 'Pods, deployments, services, and cluster management.', tags: ['pods', 'deployments', 'k8s'], emoji: '☸️', new: true, live: false },
];

const categories = ['All', 'React', 'JavaScript', 'Python', 'CSS', 'Algorithms', 'Databases', 'DevOps', 'TypeScript', 'Backend', 'System Design'];
const difficulties = ['All', 'Easy', 'Medium', 'Hard'];
const sortOptions = ['Most Popular', 'Highest Rated', 'Newest', 'Shortest'];

const difficultyBadge = (d: string) =>
  d === 'Easy' ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400' :
  d === 'Medium' ? 'bg-amber-100 dark:bg-amber-950/40 text-amber-700 dark:text-amber-400' :
  'bg-rose-100 dark:bg-rose-950/40 text-rose-700 dark:text-rose-400';

export function QuizDiscovery({ setView, setSelectedQuizId, setQuizAnswers, setQuizStarted }: AppState) {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('All');
  const [difficulty, setDifficulty] = useState('All');
  const [sort, setSort] = useState('Most Popular');
  const [showFilters, setShowFilters] = useState(false);

  const handleStart = (id: number) => {
    setSelectedQuizId(id);
    setQuizAnswers({});
    setQuizStarted(false);
    setView('user-quiz');
  };

  const filtered = allQuizzes.filter(q => {
    const matchSearch = q.title.toLowerCase().includes(search.toLowerCase()) ||
      q.category.toLowerCase().includes(search.toLowerCase()) ||
      q.tags.some(t => t.includes(search.toLowerCase()));
    const matchCat = category === 'All' || q.category === category;
    const matchDiff = difficulty === 'All' || q.difficulty === difficulty;
    return matchSearch && matchCat && matchDiff;
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-black text-slate-900 dark:text-white">Discover Quizzes</h1>
        <p className="text-slate-500 dark:text-slate-400">Browse {allQuizzes.length} quizzes across {categories.length - 1} categories</p>
      </div>

      {/* Search */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-2 flex-1 px-4 py-3 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl">
          <Search className="w-4 h-4 text-slate-400 flex-shrink-0" />
          <input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Search quizzes, topics, tags..."
            className="bg-transparent text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-600 outline-none flex-1"
          />
        </div>
        <button
          onClick={() => setShowFilters(!showFilters)}
          className={`flex items-center gap-2 px-4 py-3 rounded-xl border transition-colors ${
            showFilters ? 'bg-indigo-50 dark:bg-indigo-950/40 border-indigo-300 dark:border-indigo-700 text-indigo-600 dark:text-indigo-400' : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300'
          }`}
        >
          <Filter className="w-4 h-4" />
          Filters
          <ChevronDown className={`w-4 h-4 transition-transform ${showFilters ? 'rotate-180' : ''}`} />
        </button>
      </div>

      {/* Filter Panel */}
      {showFilters && (
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-4 space-y-4"
        >
          <div>
            <div className="text-slate-500 dark:text-slate-400 text-sm font-medium mb-2">Difficulty</div>
            <div className="flex flex-wrap gap-2">
              {difficulties.map(d => (
                <button
                  key={d}
                  onClick={() => setDifficulty(d)}
                  className={`px-3 py-1.5 rounded-xl text-sm font-medium transition-all ${
                    difficulty === d ? 'bg-indigo-500 text-white' : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'
                  }`}
                >
                  {d}
                </button>
              ))}
            </div>
          </div>
          <div>
            <div className="text-slate-500 dark:text-slate-400 text-sm font-medium mb-2">Sort By</div>
            <div className="flex flex-wrap gap-2">
              {sortOptions.map(s => (
                <button
                  key={s}
                  onClick={() => setSort(s)}
                  className={`px-3 py-1.5 rounded-xl text-sm font-medium transition-all ${
                    sort === s ? 'bg-indigo-500 text-white' : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'
                  }`}
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        </motion.div>
      )}

      {/* Category Pills */}
      <div className="flex gap-2 overflow-x-auto pb-1 no-scrollbar">
        {categories.map(cat => (
          <button
            key={cat}
            onClick={() => setCategory(cat)}
            className={`px-4 py-2 rounded-xl text-sm font-medium whitespace-nowrap transition-all flex-shrink-0 ${
              category === cat
                ? 'bg-gradient-to-r from-indigo-500 to-violet-600 text-white shadow-md shadow-indigo-500/25'
                : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:border-indigo-300 dark:hover:border-indigo-700'
            }`}
          >
            {cat}
          </button>
        ))}
      </div>

      {/* Results count */}
      <div className="flex items-center justify-between">
        <p className="text-slate-500 dark:text-slate-400 text-sm">{filtered.length} quizzes found</p>
      </div>

      {/* Quiz Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        {filtered.map((quiz, idx) => (
          <motion.div
            key={quiz.id}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: idx * 0.04 }}
            className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden hover:shadow-xl hover:-translate-y-0.5 transition-all group"
          >
            <div className="p-5">
              <div className="flex items-start justify-between mb-3">
                <div className="flex items-center gap-3">
                  <span className="text-3xl">{quiz.emoji}</span>
                  <div>
                    <div className="flex items-center gap-2 mb-0.5">
                      <h3 className="font-bold text-slate-900 dark:text-white group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">{quiz.title}</h3>
                    </div>
                    <span className="text-slate-500 dark:text-slate-400 text-sm">{quiz.category}</span>
                  </div>
                </div>
                <div className="flex flex-col items-end gap-1.5 flex-shrink-0">
                  <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${difficultyBadge(quiz.difficulty)}`}>{quiz.difficulty}</span>
                  {quiz.live && <span className="flex items-center gap-1 px-2 py-0.5 bg-rose-100 dark:bg-rose-950/40 text-rose-700 dark:text-rose-400 rounded-full text-xs font-medium"><div className="w-1.5 h-1.5 bg-rose-500 rounded-full animate-pulse" />LIVE</span>}
                  {quiz.new && !quiz.live && <span className="px-2 py-0.5 bg-indigo-100 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-400 rounded-full text-xs font-medium">NEW</span>}
                </div>
              </div>

              <p className="text-slate-500 dark:text-slate-400 text-sm mb-3 line-clamp-2">{quiz.description}</p>

              <div className="flex flex-wrap gap-1.5 mb-4">
                {quiz.tags.slice(0, 3).map(tag => (
                  <span key={tag} className="px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 rounded-lg text-xs">#{tag}</span>
                ))}
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3 text-slate-500 dark:text-slate-400 text-sm">
                  <span className="flex items-center gap-1"><BookOpen className="w-3.5 h-3.5" />{quiz.questions}</span>
                  <span className="flex items-center gap-1"><Clock className="w-3.5 h-3.5" />{quiz.duration}m</span>
                  <span className="flex items-center gap-1"><Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />{quiz.rating}</span>
                </div>
                <button
                  onClick={() => handleStart(quiz.id)}
                  className="flex items-center gap-1.5 px-4 py-2 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl text-sm font-semibold hover:from-indigo-600 hover:to-violet-700 transition-all shadow-md shadow-indigo-500/20"
                >
                  <Play className="w-3.5 h-3.5" />
                  Start
                </button>
              </div>
            </div>
          </motion.div>
        ))}
      </div>

      {filtered.length === 0 && (
        <div className="text-center py-16">
          <div className="text-5xl mb-4">🔍</div>
          <h3 className="font-bold text-slate-900 dark:text-white mb-2">No quizzes found</h3>
          <p className="text-slate-500 dark:text-slate-400">Try adjusting your search or filters</p>
        </div>
      )}
    </div>
  );
}

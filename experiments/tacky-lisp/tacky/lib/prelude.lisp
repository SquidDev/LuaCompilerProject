;; Creates a top-level function
(define-macro defun (lambda (name args ...)
  `(define ,name (lambda ,args ,@...))))

;; Create a top-level macro
(define-macro defmacro (lambda (name args ...)
  `(define-macro ,name (lambda ,args ,@...))))

;; Creates a code block, for use where an expression would normally be required.
(defmacro progn (...)
  `((lambda () ,@...)))

;; Evaluates a condition, evaluating the second argument if truthy, the third
;; argument if falsey.
(defmacro if (c t b) `(cond (,c ,t) (true ,b)))

;; Create a random symbol
(define-native gensym)

;; Perform an action whilst a value is true
(defmacro while (check ...)
  (let* ((impl (gensym)))
    `(progn
      (letrec ((,impl (lambda ()
                        (cond
                          (,check ,@... (,impl))
                          (true)))))
        (,impl)))))

(defmacro for (ctr start end step ...)
  (let* ((impl (gensym))
         (ctr' (gensym))
         (end' (gensym))
         (step' (gensym)))
    `(let* ((,end' ,end)
              (,step' ,step)
              (,impl nil))
       (set! ,impl (lambda (,ctr')
                     (cond
                       ((if (< 0 ,step) (<= ,ctr' ,end') (>= ,ctr' ,end'))
                         (let* ((,ctr ,ctr')) ,@...)
                         (,impl (+ ,ctr' ,step')))
                       (true))))
       (,impl ,start))))

(defun ! (expr) (cond (expr false) (true true)))

;; Print one or more values to standard output
(define-native print!)

;; Pretty-print one or more values to standard ouput
(define-native pretty-print!)

(define-native dump-node!)
(defun debug! (x)
  (dump-node! x)
  x)

;; Get a table key
(define-native get-idx)

;; Set a table key to a value
(define-native set-idx!)

(define-native ==)
(define-native ~=)
(define-native <)
(define-native <=)
(define-native >)
(define-native >=)

(define-native +)
(define-native -)
(define-native *)
(define-native /)
(define-native %)
(define-native ^)

(define-native and)
(define-native or)

(define-native invoke-dynamic)
(define-native type)

(defun list? (x) (== (type x) "list"))

;; Check if this is a list and it is empty
(defun nil? (x) (and (list? x) (== (# x) 0)))

(define /= ~=)
(define = ==)

;; Get the length of a list
(defun # (li) (get-idx li "n"))

;; Push an entry on to the end of this list
(defun push-cdr! (li val)
  (let* ((len (+ (# li) 1)))
    (set-idx! li "n" len)
    (set-idx! li len val)
    li))

;; Build a list from the arguments
(defun list (...) ...)

;; Map a function over every item in the list, creating a new list
(defun map (fn li)
  (let* ((out '()))
    (set-idx! out "n" (# li))
    (for i 1 (# li) 1 (set-idx! out i (fn (get-idx li i))))
    out))

(defun traverse (li fn) (map fn li))

(defun car (xs) (get-idx xs 1))
(define-native cdr)

(defun cars (xs) (map car xs))
(defun cdrs (xs) (map cdr xs))

(defun caar (x) (car (car x)))
(defun cadr (x) (car (cdr x)))
(defun cdar (x) (cdr (car x)))
(defun cddr (x) (cdr (cdr x)))
(defun caaar (x) (car (car (car x))))
(defun caadr (x) (car (car (cdr x))))
(defun cadar (x) (car (cdr (car x))))
(defun caddr (x) (car (cdr (cdr x))))
(defun cdaar (x) (cdr (car (car x))))
(defun cdadr (x) (cdr (car (cdr x))))
(defun cddar (x) (cdr (cdr (car x))))
(defun cdddr (x) (cdr (cdr (cdr x))))
(defun caaaar (x) (car (car (car (car x)))))
(defun caaadr (x) (car (car (car (cdr x)))))
(defun caadar (x) (car (car (cdr (car x)))))
(defun caaddr (x) (car (car (cdr (cdr x)))))
(defun cadaar (x) (car (cdr (car (car x)))))
(defun cadadr (x) (car (cdr (car (cdr x)))))
(defun caddar (x) (car (cdr (cdr (car x)))))
(defun cadddr (x) (car (cdr (cdr (cdr x)))))
(defun cdaaar (x) (cdr (car (car (car x)))))
(defun cdaadr (x) (cdr (car (car (cdr x)))))
(defun cdadar (x) (cdr (car (cdr (car x)))))
(defun cdaddr (x) (cdr (car (cdr (cdr x)))))
(defun cddaar (x) (cdr (cdr (car (car x)))))
(defun cddadr (x) (cdr (cdr (car (cdr x)))))
(defun cdddar (x) (cdr (cdr (cdr (car x)))))
(defun cddddr (x) (cdr (cdr (cdr (cdr x)))))

(defun caars (xs) (map caar xs))
(defun cadrs (xs) (map cadr xs))
(defun cdars (xs) (map cdar xs))
(defun cddrs (xs) (map cddr xs))
(defun caaars (xs) (map caaar xs))
(defun caadrs (xs) (map caadr xs))
(defun cadars (xs) (map cadar xs))
(defun caddrs (xs) (map caddr xs))
(defun cdaars (xs) (map cdaar xs))
(defun cdadrs (xs) (map cdadr xs))
(defun cddars (xs) (map cddar xs))
(defun cdddrs (xs) (map cdddr xs))
(defun caaaars (xs) (map caaaar xs))
(defun caaadrs (xs) (map caaadr xs))
(defun caadars (xs) (map caadar xs))
(defun caaddrs (xs) (map caaddr xs))
(defun cadaars (xs) (map cadaar xs))
(defun cadadrs (xs) (map cadadr xs))
(defun caddars (xs) (map caddar xs))
(defun cadddrs (xs) (map cadddr xs))
(defun cdaaars (xs) (map cdaaar xs))
(defun cdaadrs (xs) (map cdaadr xs))
(defun cdadars (xs) (map cdadar xs))
(defun cdaddrs (xs) (map cdaddr xs))
(defun cddaars (xs) (map cddaar xs))
(defun cddadrs (xs) (map cddadr xs))
(defun cdddars (xs) (map cdddar xs))
(defun cddddrs (xs) (map cddddr xs))

;; Binds a variable to an expression
(defmacro let (vars ...)
  `((lambda ,(cars vars) ,@...) ,@(cadrs vars)))

(defmacro let* (vars ...)
  ; Note, this depends on as few library functions as possible: it is used
  ; by most macros to "bootstrap" then language.
  (if (! (nil? vars))
    `((lambda (,(caar vars)) (let* ,(cdr vars) ,@...)) ,(cadar vars))
    `((lambda () ,@...))))

(defmacro letrec (vars ...)
  `((lambda ,(cars vars)
    ,@(map (lambda (var) `(set! ,(car var) ,(cadr var))) vars)
    ,@...)))

;; Binds a single variable
(defmacro with (var ...) `(let (,var) ,@...))

;; Return a new list where only the predicate matches
(defun filter (fn li)
  (with (out '())
    (for i 1 (# li 1) 1 (let ((item (get-idx li i)))
      (if (fn item) (push-cdr! out item))))
    out))

;; Determine whether any element matches a predicate
(defun any (fn li)
  (letrec ((any-impl (lambda (i)
                       (cond
                         ((>= i (# li)) false)
                         ((fn (get-idx li i)) true)
                         (true (any-impl (+ i 1)))))))
    (any-impl 1)))

;; Determine whether all elements match a predicate
(defun all (fn li)
  (letrec ((all-impl (lambda(i)
                       (cond
                         ((>= i (# li)) true)
                         ((fn (get-idx li i)) (all-impl (+ i 1)))
                         (true false)))))
    (all-impl 1)))

;; Fold left across a list
(defun foldl (func accum li)
  (for i 1 (# li) 1
    (set! accum (func accum (get-idx li i))))
  accum)

;; Fold right across a list
(defun foldr (func accum li)
  (for i (# li) 1 -1
    (set! accum (func (get-idx li i) accum)))
  accum)

(defmacro case (x ...)
  (let* ((cases ...)
         (name (gensym))
         (transform-case (lambda (case)
                           (if (list? case)
                             `((,@(car case) ,name) ,@(cdr case))
                             `(true)))))
    `((lambda (,name) (cond ,@(map transform-case cases))) ,x)))

(defmacro -> (x ...)
  (if (/= (# ...) 0)
    (let* ((form (car ...))
           (threaded (cond
                       ((list? form) `(,(car form) ,x ,@(cdr form)))
                       (true `(,form ,x)))))
      `(-> ,threaded ,@(cdr ...)))
    x))

(defmacro ->> (x ...)
  (if (/= (# ...) 0)
    (let* ((form (car ...))
           (threaded (cond
                       ((list? form) `(,@form ,x))
                       (true `(,form ,x)))))
      `(->> ,threaded ,@(cdr ...)))
    x))

(defun succ (x) (+ 1 x))
(defun pred (x) (- x 1))

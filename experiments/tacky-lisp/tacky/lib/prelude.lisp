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
  (define impl (gensym))
  `(progn
    (defun ,impl () (cond
      (,check ,@... (,impl))
      (true)))
    (,impl)))

(defmacro for (ctr start end step ...)
  (define impl (gensym))
  (define ctr' (gensym))
  (define end' (gensym))
  (define step' (gensym))

  `(progn
    (define ,end' ,end)
    (define ,step' ,step)
    (defun ,impl (,ctr') (cond
      ((if (< 0 ,step) (<= ,ctr' ,end') (>= ,ctr' ,end'))
        (define ,ctr ,ctr')
        ,@...
        (,impl (+ ,ctr' ,step')))
      (true)))
    (,impl ,start)))

(defun ! (expr) (cond (expr false) (true true)))

;; Print one or more values to standard output
(define-native print!)

;; Pretty-print one or more values to standard ouput
(define-native pretty-print!)

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

(define != ~=)

;; Get the length of a list
(defun # (li) (get-idx li "n"))

;; Check if the list is empty
(defun empty? (li) (== (# li) 0))

;; Push an entry on to the end of this list
(defun push-cdr! (li val)
  (define len (+ (# li) 1))
  (set-idx! li "n" len)
  (set-idx! li len val)
  li)

;; Build a list from the arguments
(defun list (...) ...)

;; Map a function over every item in the list, creating a new list
(defun map (fn li)
  (define out '())
  (set-idx! out "n" (# li))
  (for i 1 (# li) 1 (set-idx! out i (fn (get-idx li i))))
  out)

(defun traverse (li fn)
  (define out '())
  (set-idx! out "n" (# li))
  (for i 1 (# li) 1 (set-idx! out i (fn (get-idx li i))))
  out)

(defun car (xs) (get-idx xs 1))
(define-native cdr)

(defun cadr (xs) (car (cdr xs)))
(defun cddr (xs) (cdr (cdr xs)))

(defun cars (xs)
  (define out '())
  (for i 1 (# xs) 1
    (push-cdr! out (car (get-idx xs i))))
  out)

(defun cdrs (xs)
  (define out '())
  (for i 1 (# xs) 1
    (push-cdr! out (cdr (get-idx xs i))))
  out)

;; Binds a variable to an expression
(defmacro let (vars ...)
  (define vas (cars vars))
  (define vds (cdrs vars))
  `((lambda ,vas ,@...) ,@vds))

;; Return a new list where only the predicate matches
(defun filter (fn li)
  (define out '())
  (for i 1 (# li 1) 1 (let ((item (get-idx li i)))
    (if (fn item) (push-cdr! out item))))
  out)

;; Determine whether any element matches a predicate
(defun any (fn li)
  (defun any-impl (i)
    (cond
      ((>= i (# li)) false)
      ((fn (get-idx li i)) true)
      (true (any-impl (+ i 1)))))
  (any-impl 1))

;; Determine whether all elements match a predicate
(defun all (fn li)
  (defun all-impl (i)
    (cond
      ((>= i (# li)) true)
      ((fn (get-idx li i)) (all-impl (+ i 1)))
      (true false)))
  (all-impl 1))

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



; vim: ft=lisp et ts=2

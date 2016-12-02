(let ((x (if "cond" 5 10)))
  (pretty-print! x)

  (while (> x 0)
    (set! x (- x 1))
    (print! x)))

(with (x 1) (pretty-print! x))

(for x 1 5 1 (print! x))

(print! "Map")
(pretty-print! (map (lambda (x) (^ x 2)) (list 1 2 3 4))) ; (1 4 9 16)

(print! "Filter")
(pretty-print! (filter (lambda (x) (== 0 (% x 2))) (list 1 2 3 4))) ; (2 4)

(print! "Any")
(pretty-print! (any (lambda (x) (== x 1)) (list 1 2 3 4))) ; True
(pretty-print! (any (lambda (x) (== x 5)) (list 1 2 3 4))) ; False

(print! "All")
(pretty-print! (all (lambda (x) (~= x 5)) (list 1 2 3 4))) ; True
(pretty-print! (all (lambda (x) (~= x 1)) (list 1 2 3 4))) ; False

(print! "Foldl")
(pretty-print! (foldl (lambda (x y) (list y x)) 0 (list 1 2 3 4)))

(print! "Foldr")
(pretty-print! (foldr (lambda (x y) (list x y)) 0 (list 1 2 3 4)))


(print! "Cond compilation")
(defun No-change () (cond
  ("hello" 0)
  ("world" 1)))

(defun Trailing-true () (cond
  ("hello" 0)
  ("world" 1)
  (true true)))

(defun Mid-way-true () (cond
  ("hello" 0)
  (true true)
  ("world" 1)))

(defun First-true () (cond
  (true true)
  ("hello" 0)
  ("world" 1)))

(defun Nested-conds-second () (cond
  ("hello" 0)
  ((cond
    ("foo" 0)
    ("bar" 10)) 1)
  ("world" 2)))

(defun Nested-conds-first () (cond
  ((cond
    ("foo" 0)
    ("bar" 10)) 0)
  ("hello" 1)))

(defun Nested-conds-many () (cond
  ((cond
    ("foo" 0)
    ("bar" 10)) 0)
  ((cond
    ("foo" 0)
    ("bar" 10)) 1)
  ((cond
    ("foo" 0)
    ("bar" 10)) 2)
  ("hello" 1)))

(print!
  (case 1
    ((>= 1) "derp")
    ((= 1) "herp")))

(defun partial (func param) (lambda (x) (func param x)))

(print! "->")
(print! (-> 1 succ succ succ))
(print! (-> '(1 2 3)
  (map succ <>)
  (map pred <>)
  (foldl + 0 <>)
  (partial + 2)))

(print!
  (letrec
    (
      (even? (lambda (x) (if (== x 0) true (odd? (- x 1)))))
      (odd? (lambda (x) (if (== x 0) false (even? (- x 1))))))
    (even? 10)))

;; Basic let*
(print!
  (let* ((a 1)
         (b 2)
         (c (+ a b)))
    (print! a)
    (print! b)
    c))

(print! "Expecting 3"
  (let* ((a 1)
         (a 2)
         (a 3))
    a))

;; Check simple importing
(import test-import)
(test-import/foobar)

;; Double check that all of these are the same
(import prelude)
(import tacky/lib/prelude)

(print! map prelude/map tacky/lib/prelude/map)

;; This should cause the code to fail to compile
; (define-macro foo (lambda (x) (bar x)))
; (define bar (lambda (x) (baz x)))
; (define baz (lambda (x) (foo x)))

(define inc' (cut + <> 1))
(define inc'' (cute + <> 1))

(pretty-print! (struct
  "foo" 1
  "bar" 2
  "baz" 3
))

(import string)
(print! (string/upper "foo"))
(pretty-print! (string/split "foo\nbar\n" "\n"))

(print! (string/.. "foo" "bar" "baz"))
